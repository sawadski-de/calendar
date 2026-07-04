using Domain;

namespace Application.Sync;

/// <summary>
/// One participant of a synced provider event who may or may not be a known team member — resolving
/// that against <see cref="Person"/> is the caller's job (<see cref="CalendarSyncService"/>), not this
/// provider's, since <c>ICalendarProvider</c> must stay ignorant of internal identity.
/// </summary>
public record ExternalAttendee(string Email, string? DisplayName);

/// <summary>
/// One event as returned by a provider snapshot — already expanded to a single occurrence (AD-15):
/// a recurring series arrives as one <see cref="ExternalCalendarEvent"/> per instance, never as one
/// row for the whole series.
/// </summary>
public record ExternalCalendarEvent(
    string ProviderEventId,
    string Title,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    bool IsAllDay,
    string? Location,
    IReadOnlyList<ExternalAttendee> Attendees);

/// <summary>The time range a sync cycle asks a provider to snapshot.</summary>
public record SyncWindow(DateTimeOffset RangeStartUtc, DateTimeOffset RangeEndUtc);

/// <summary>
/// The only port the Worker knows about for reading an external calendar (AD-2). Exactly one method,
/// and it is read-only by construction — there is no <c>CreateEvent</c>/<c>UpdateEvent</c>/
/// <c>DeleteEvent</c> on this interface, and there must never be one added: that is what makes FR-7
/// ("kein Zurückschreiben") a structural guarantee rather than a convention every adapter has to
/// remember to honor.
/// </summary>
public interface ICalendarProvider
{
    /// <summary>
    /// Returns the full current snapshot of <paramref name="connection"/>'s events inside
    /// <paramref name="window"/> — never a delta/incremental result. <see cref="CalendarSyncService"/>
    /// diffs this snapshot against what's already stored to decide inserts/updates/deletes.
    /// </summary>
    Task<IReadOnlyList<ExternalCalendarEvent>> FetchAllEventsAsync(
        CalendarConnection connection,
        SyncWindow window,
        CancellationToken cancellationToken = default);
}
