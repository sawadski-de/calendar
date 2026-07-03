namespace Domain;

/// <summary>
/// An internal team member attending an <see cref="Appointment"/>. Native appointment creation (Story
/// 1.3) only ever attaches attendees picked from the team roster — there is no external/free-text
/// invite path, so <see cref="PersonId"/> always resolves to a real <see cref="Person"/>.
/// </summary>
public class Attendee
{
    public Guid Id { get; private set; }
    public Guid AppointmentId { get; private set; }
    public Guid PersonId { get; private set; }

    public Attendee(Guid id, Guid appointmentId, Guid personId)
    {
        Id = id;
        AppointmentId = appointmentId;
        PersonId = personId;
    }
}
