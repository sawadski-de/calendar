using Application.Accounts;
using Application.Appointments;
using Domain;

namespace Infrastructure.Appointments;

public class AppointmentCreationService(IPersonRepository personRepository, IAppointmentRepository appointmentRepository)
    : IAppointmentCreationService
{
    public async Task<AppointmentCreationResult> CreateNativeAppointmentAsync(
        Guid ownerId,
        string title,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        IReadOnlyCollection<Guid> attendeePersonIds,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return new AppointmentCreationResult(false, null, "title-required");
        }

        if (endUtc <= startUtc)
        {
            return new AppointmentCreationResult(false, null, "invalid-time-range");
        }

        foreach (var attendeePersonId in attendeePersonIds)
        {
            if (await personRepository.GetByIdAsync(attendeePersonId, cancellationToken) is null)
            {
                return new AppointmentCreationResult(false, null, "attendee-not-found");
            }
        }

        // Native appointment: Provider/ProviderEventId both NULL (AD-7).
        var appointment = new Appointment(Guid.NewGuid(), ownerId, title, startUtc, endUtc);
        foreach (var attendeePersonId in attendeePersonIds)
        {
            appointment.AddAttendee(attendeePersonId);
        }

        appointment.AssignStatus(StatusHeuristicService.Compute(appointment));

        await appointmentRepository.AddAsync(appointment, cancellationToken);
        return new AppointmentCreationResult(true, appointment, null);
    }
}
