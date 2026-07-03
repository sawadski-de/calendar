using Domain;

namespace Application.Appointments;

/// <summary>
/// The write side of appointment persistence — separate from the read-only
/// <see cref="IAppointmentViewService"/> (AD-3 scopes that interface to reads only).
/// </summary>
public interface IAppointmentRepository
{
    Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);
}
