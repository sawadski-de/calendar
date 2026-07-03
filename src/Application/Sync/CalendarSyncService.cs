using Application.Appointments;
using Domain;

namespace Application.Sync;

public enum SyncOutcome
{
    Succeeded,
    Failed,
}

/// <summary>
/// Orchestrates one <see cref="CalendarConnection"/>'s sync cycle — the single place the
/// snapshot-diff/upsert/delete logic (AD-7) and status computation (AD-4) live. Both
/// <c>GoogleCalendarProvider</c> (Story 2.1) and <c>OutlookCalendarProvider</c> (Story 2.2) plug in
/// via <see cref="ICalendarProvider"/>, resolved per connection through <see cref="ICalendarProviderResolver"/>
/// (Story 2.2 — this service originally took a single injected provider, which only worked with
/// exactly one provider in the system), so there is exactly one upsert codepath, not two.
/// </summary>
public class CalendarSyncService(
    ICalendarProviderResolver calendarProviderResolver,
    IAppointmentRepository appointmentRepository,
    ICalendarConnectionRepository calendarConnectionRepository,
    TimeProvider timeProvider)
{
    public async Task<SyncOutcome> SyncAsync(
        CalendarConnection connection,
        SyncWindow window,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        // The whole cycle — fetch AND diff/persist — is one failure domain (code review finding: the
        // original version only caught exceptions around the fetch call and only CalendarProviderException,
        // so a DB error during ApplySyncResultAsync or a token-decryption failure during fetch propagated
        // uncaught past this method, meaning CalendarConnection.RecordFailure never ran and the Settings
        // UI kept showing a healthy connection while sync was silently and permanently broken).
        try
        {
            var calendarProvider = calendarProviderResolver.Resolve(connection.Provider);
            var events = await calendarProvider.FetchAllEventsAsync(connection, window, cancellationToken);

            var existingByProviderEventId = await appointmentRepository.GetSyncedAppointmentsAsync(
                connection.PersonId, connection.Provider, cancellationToken);

            var toInsert = new List<Appointment>();
            var toDelete = new List<Guid>();
            var seenProviderEventIds = new HashSet<string>();

            foreach (var evt in events)
            {
                seenProviderEventIds.Add(evt.ProviderEventId);
                existingByProviderEventId.TryGetValue(evt.ProviderEventId, out var current);

                if (current is not null && !HasChanged(current, evt))
                {
                    continue;
                }

                if (current is not null)
                {
                    // "Update" is modeled as replace (delete + insert with a fresh Id) — Appointment has no
                    // in-place setters for title/time/location beyond construction (Domain, Story 1.3).
                    toDelete.Add(current.Id);
                }

                var appointment = new Appointment(
                    Guid.NewGuid(),
                    connection.PersonId,
                    evt.Title,
                    evt.StartUtc,
                    evt.EndUtc,
                    connection.Provider,
                    evt.ProviderEventId,
                    evt.IsAllDay,
                    evt.Location);

                foreach (var attendee in evt.Attendees)
                {
                    appointment.AddExternalAttendee(attendee.Email, attendee.DisplayName);
                }

                appointment.AssignStatus(StatusHeuristicService.Compute(appointment));
                toInsert.Add(appointment);
            }

            foreach (var (providerEventId, appointment) in existingByProviderEventId)
            {
                if (!seenProviderEventIds.Contains(providerEventId))
                {
                    toDelete.Add(appointment.Id);
                }
            }

            await appointmentRepository.ApplySyncResultAsync(toInsert, toDelete, cancellationToken);
            connection.RecordSyncSuccess(now);
            await calendarConnectionRepository.UpsertAsync(connection, cancellationToken);
            return SyncOutcome.Succeeded;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A shutdown/cancellation is not a sync failure — must not be recorded as one, and must
            // propagate so the Worker's cycle loop can react to it normally.
            throw;
        }
        catch (Exception ex)
        {
            // CalendarProviderException carries a stable, small error code (AD-13 spirit); anything
            // else (DB errors, token-decryption failures, an unexpected provider response shape) still
            // has to count against ConsecutiveFailureCount — falling back to a generic code rather than
            // letting it escape uncaught and leave the connection's error state stuck at "healthy".
            var errorCode = ex is CalendarProviderException providerEx ? providerEx.ErrorCode : "unknown_error";
            connection.RecordFailure(now, errorCode);
            await calendarConnectionRepository.UpsertAsync(connection, cancellationToken);
            return SyncOutcome.Failed;
        }
    }

    private static bool HasChanged(Appointment current, ExternalCalendarEvent evt)
    {
        if (current.Title != evt.Title
            || current.StartUtc != evt.StartUtc
            || current.EndUtc != evt.EndUtc
            || current.IsAllDay != evt.IsAllDay
            || current.Location != evt.Location)
        {
            return true;
        }

        // Compare both email (case-insensitive — a provider normalizing casing between syncs must not
        // look like a "changed" attendee) and display name (code review finding: the original version
        // only diffed emails, so a provider-side display-name update — email unchanged — was silently
        // dropped and never persisted).
        var currentAttendees = current.Attendees
            .Select(a => ((a.ExternalEmail ?? string.Empty).ToUpperInvariant(), a.ExternalDisplayName))
            .OrderBy(a => a.Item1, StringComparer.Ordinal);
        var newAttendees = evt.Attendees
            .Select(a => (a.Email.ToUpperInvariant(), a.DisplayName))
            .OrderBy(a => a.Item1, StringComparer.Ordinal);
        return !currentAttendees.SequenceEqual(newAttendees);
    }
}

/// <summary>
/// Raised by an <see cref="ICalendarProvider"/> implementation for any failure that should count
/// against <see cref="CalendarConnection.ConsecutiveFailureCount"/> (AD-16) — <see cref="ErrorCode"/>
/// is a small stable set (e.g. <c>"token_refresh_failed"</c>, <c>"provider_error"</c>), never a raw
/// exception message (AD-13's "stable code, not free text" spirit applies here too).
/// </summary>
public class CalendarProviderException(string errorCode, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public string ErrorCode { get; } = errorCode;
}
