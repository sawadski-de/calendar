using Application.Appointments;
using Domain;
using Infrastructure.Persistence;

namespace Infrastructure.Appointments;

public class AppointmentRepository(ApplicationDbContext dbContext) : IAppointmentRepository
{
    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
