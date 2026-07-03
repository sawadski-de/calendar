using Domain;

namespace Application.Appointments;

/// <summary>
/// The single read path for appointment data (AD-3). This story only ever returns the caller's own
/// appointments — Epic 3 adds the Privat-Default-filtered bulk method (<c>GetForViewers</c>) alongside
/// this one; the shape here is chosen so that addition doesn't require restructuring this interface.
/// </summary>
public interface IAppointmentViewService
{
    Task<IReadOnlyList<Appointment>> GetOwnAppointmentsAsync(
        Guid personId,
        DateTimeOffset rangeStartUtc,
        DateTimeOffset rangeEndUtc,
        CancellationToken cancellationToken = default);
}
