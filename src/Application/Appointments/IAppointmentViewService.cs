using Domain;

namespace Application.Appointments;

/// <summary>
/// The single read path for appointment data (AD-3). <see cref="GetForViewerByIdAsync"/> is the one
/// privacy-aware method for a single appointment — it serves both an owner's own appointment (always
/// full) and a colleague's (full only if the viewer is a listed attendee, else status-only, FR-9) so no
/// second codepath ever reimplements the Privat-Default decision. <see cref="GetForViewersAsync"/> is
/// the bulk, always-status-only fetch used to populate colleague calendar columns (Story 3.1, AC 7).
/// </summary>
public interface IAppointmentViewService
{
    Task<IReadOnlyList<Appointment>> GetOwnAppointmentsAsync(
        Guid personId,
        DateTimeOffset rangeStartUtc,
        DateTimeOffset rangeEndUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <c>null</c> only when the appointment genuinely doesn't exist (404). Otherwise
    /// <see cref="AppointmentViewResult.IsFullDetail"/> is <c>true</c> when <paramref name="viewerId"/>
    /// owns the appointment or is a listed attendee (FR-9 exception, applies to native and synced
    /// appointments alike) — in that case <see cref="AppointmentViewResult.FullAppointment"/> is
    /// populated. Otherwise only <see cref="AppointmentViewResult.Status"/> is meaningful.
    /// </summary>
    Task<AppointmentViewResult?> GetForViewerByIdAsync(
        Guid viewerId,
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Status-only slots for one or more colleagues' calendars, keyed by <c>PersonId</c> — a person ID
    /// with no appointments in range simply has no key in the result (callers default to an empty
    /// list). Never includes a title/attendees/location field, regardless of attendee status — the
    /// bulk calendar-column view always renders status blocks only (Story 3.1, AC 3); the FR-9
    /// attendee exception only ever surfaces through <see cref="GetForViewerByIdAsync"/>'s detail view.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<ViewerAppointmentSlot>>> GetForViewersAsync(
        IReadOnlyList<Guid> personIds,
        DateTimeOffset rangeStartUtc,
        DateTimeOffset rangeEndUtc,
        CancellationToken cancellationToken = default);
}
