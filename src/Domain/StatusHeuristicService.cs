namespace Domain;

/// <summary>
/// The single implementation of the availability-status heuristic (AD-4/AD-5) — called once at write
/// time (native creation, and later Epic 2's sync upsert), never recomputed on read.
/// </summary>
public static class StatusHeuristicService
{
    private static readonly TimeSpan ShortMeetingCeiling = TimeSpan.FromMinutes(45);
    private static readonly TimeSpan LongMeetingFloor = TimeSpan.FromMinutes(90);
    private const int LongMeetingAttendeeFloor = 3;

    /// <summary>
    /// Rule order is load-bearing — the no-attendee rule must be checked first, since it outranks
    /// every other rule regardless of duration or <see cref="Appointment.IsAllDay"/>.
    /// </summary>
    public static AvailabilityStatus Compute(Appointment appointment)
    {
        var attendeeCount = appointment.Attendees.Count;
        if (attendeeCount == 0)
        {
            return AvailabilityStatus.Unterbrechbar;
        }

        var duration = appointment.EndUtc - appointment.StartUtc;
        if (duration <= ShortMeetingCeiling)
        {
            return AvailabilityStatus.Unterbrechbar;
        }

        if (appointment.IsAllDay || (duration >= LongMeetingFloor && attendeeCount >= LongMeetingAttendeeFloor))
        {
            return AvailabilityStatus.BitteNichtStoeren;
        }

        return AvailabilityStatus.Unterbrechbar;
    }
}
