using Application.Accounts;
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
    IPersonRepository personRepository,
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

            // A synced attendee whose email matches a team member's is attached as that Person (not
            // stored as external) — so Epic 3's Mehrpersonen-Ansicht recognizes them as a real
            // teammate rather than an opaque external label. Fetched once per cycle, not per event.
            var roster = await personRepository.GetAllAsync(cancellationToken);
            var rosterByEmail = roster.ToDictionary(p => p.Email.ToUpperInvariant(), p => p);

            var toInsert = new List<Appointment>();
            var toDelete = new List<Guid>();
            var seenProviderEventIds = new HashSet<string>();

            foreach (var evt in events)
            {
                seenProviderEventIds.Add(evt.ProviderEventId);
                existingByProviderEventId.TryGetValue(evt.ProviderEventId, out var current);

                if (current is not null && !HasChanged(current, evt, rosterByEmail))
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
                    AttachAttendee(appointment, attendee, rosterByEmail);
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

    private static bool HasChanged(Appointment current, ExternalCalendarEvent evt, IReadOnlyDictionary<string, Person> rosterByEmail)
    {
        if (current.Title != evt.Title
            || current.StartUtc != evt.StartUtc
            || current.EndUtc != evt.EndUtc
            || current.IsAllDay != evt.IsAllDay
            || current.Location != evt.Location)
        {
            return true;
        }

        // Same resolution AttachAttendee uses (roster-matched → Person key, else email+displayName
        // key) — this is what makes a team member joining the roster between syncs (an attendee that
        // used to be external now matches) correctly look like a change, without a separate codepath.
        var currentKeys = current.Attendees.Select(AttendeeKey).OrderBy(k => k, StringComparer.Ordinal);
        var newKeys = evt.Attendees.Select(a => ResolveAttendeeKey(a, rosterByEmail)).OrderBy(k => k, StringComparer.Ordinal);
        return !currentKeys.SequenceEqual(newKeys);
    }

    /// <summary>
    /// Attaches a synced event's attendee as a real team member (<see cref="Appointment.AddAttendee"/>)
    /// when their email matches the roster, otherwise as an external participant
    /// (<see cref="Appointment.AddExternalAttendee"/>) — the one place this decision is made, shared by
    /// the insert path and <see cref="HasChanged"/>'s diff via <see cref="ResolveAttendeeKey"/>.
    /// </summary>
    private static void AttachAttendee(Appointment appointment, ExternalAttendee attendee, IReadOnlyDictionary<string, Person> rosterByEmail)
    {
        if (rosterByEmail.TryGetValue(attendee.Email.ToUpperInvariant(), out var person))
        {
            appointment.AddAttendee(person.Id);
        }
        else
        {
            appointment.AddExternalAttendee(attendee.Email, attendee.DisplayName);
        }
    }

    private static string AttendeeKey(Attendee attendee) =>
        attendee.PersonId is { } personId
            ? $"P:{personId}"
            : $"E:{(attendee.ExternalEmail ?? string.Empty).ToUpperInvariant()}|{attendee.ExternalDisplayName}";

    private static string ResolveAttendeeKey(ExternalAttendee attendee, IReadOnlyDictionary<string, Person> rosterByEmail) =>
        rosterByEmail.TryGetValue(attendee.Email.ToUpperInvariant(), out var person)
            ? $"P:{person.Id}"
            : $"E:{attendee.Email.ToUpperInvariant()}|{attendee.DisplayName}";
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
