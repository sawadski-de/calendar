using Application.Appointments;
using Domain;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Appointments;

public class AppointmentViewService(ApplicationDbContext dbContext) : IAppointmentViewService
{
    public async Task<IReadOnlyList<Appointment>> GetOwnAppointmentsAsync(
        Guid personId,
        DateTimeOffset rangeStartUtc,
        DateTimeOffset rangeEndUtc,
        CancellationToken cancellationToken = default)
    {
        // Overlap test, not containment: an appointment that starts before the range but is still
        // active inside it (e.g. spanning midnight into a Day view) must still be returned.
        return await dbContext.Appointments
            .Where(a => a.PersonId == personId && a.StartUtc < rangeEndUtc && a.EndUtc > rangeStartUtc)
            .OrderBy(a => a.StartUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<Appointment?> GetOwnAppointmentByIdAsync(
        Guid personId,
        Guid appointmentId,
        CancellationToken cancellationToken = default) =>
        dbContext.Appointments
            .Include(a => a.Attendees)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.PersonId == personId, cancellationToken);
}
