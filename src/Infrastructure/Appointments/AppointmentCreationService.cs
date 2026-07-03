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

        // A duplicate id in the request (double-submit, buggy client) must not reach AddAttendee twice —
        // the unique (AppointmentId, PersonId) index would turn that into an unhandled 500 instead of a
        // clean validation response. Distinct() here also lets attendee-existence be checked against a
        // single roster fetch instead of one DB round-trip per attendee.
        var distinctAttendeePersonIds = attendeePersonIds.Distinct().ToList();

        var roster = await personRepository.GetAllAsync(cancellationToken);
        var rosterIds = roster.Select(p => p.Id).ToHashSet();
        if (distinctAttendeePersonIds.Any(id => !rosterIds.Contains(id)))
        {
            return new AppointmentCreationResult(false, null, "attendee-not-found");
        }

        // Native appointment: Provider/ProviderEventId both NULL (AD-7).
        var appointment = new Appointment(Guid.NewGuid(), ownerId, title, startUtc, endUtc);
        foreach (var attendeePersonId in distinctAttendeePersonIds)
        {
            appointment.AddAttendee(attendeePersonId);
        }

        appointment.AssignStatus(StatusHeuristicService.Compute(appointment));

        await appointmentRepository.AddAsync(appointment, cancellationToken);
        return new AppointmentCreationResult(true, appointment, null);
    }
}
