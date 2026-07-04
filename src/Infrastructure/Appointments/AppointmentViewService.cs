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

    public async Task<AppointmentViewResult?> GetForViewerByIdAsync(
        Guid viewerId,
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var appointment = await dbContext.Appointments
            .Include(a => a.Attendees)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            return null;
        }

        var isFullDetail = appointment.PersonId == viewerId || appointment.Attendees.Any(a => a.PersonId == viewerId);
        return new AppointmentViewResult(appointment.Id, isFullDetail, isFullDetail ? appointment : null, appointment.Status);
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<ViewerAppointmentSlot>>> GetForViewersAsync(
        IReadOnlyList<Guid> personIds,
        DateTimeOffset rangeStartUtc,
        DateTimeOffset rangeEndUtc,
        CancellationToken cancellationToken = default)
    {
        if (personIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<ViewerAppointmentSlot>>();
        }

        var slots = await dbContext.Appointments
            .Where(a => personIds.Contains(a.PersonId) && a.StartUtc < rangeEndUtc && a.EndUtc > rangeStartUtc)
            .OrderBy(a => a.StartUtc)
            .Select(a => new ViewerAppointmentSlot(a.PersonId, a.Id, a.StartUtc, a.EndUtc, a.Status))
            .ToListAsync(cancellationToken);

        return slots
            .GroupBy(s => s.PersonId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ViewerAppointmentSlot>)g.ToList());
    }
}
