namespace Domain;

/// <summary>
/// A participant of an <see cref="Appointment"/> — either an internal team member
/// (<see cref="PersonId"/> set, native creation, Story 1.3) or an external participant carried over
/// from a synced provider event (<see cref="ExternalEmail"/> set, Story 2.1) who has no <see cref="Person"/>
/// row. Exactly one of the two is set — enforced by a DB check constraint, not in this constructor,
/// since EF Core materializes entities via the parameterless-friendly constructor path too.
/// </summary>
public class Attendee
{
    public Guid Id { get; private set; }
    public Guid AppointmentId { get; private set; }
    public Guid? PersonId { get; private set; }
    public string? ExternalEmail { get; private set; }
    public string? ExternalDisplayName { get; private set; }

    public Attendee(Guid id, Guid appointmentId, Guid personId)
    {
        Id = id;
        AppointmentId = appointmentId;
        PersonId = personId;
    }

    private Attendee(Guid id, Guid appointmentId, string externalEmail, string? externalDisplayName)
    {
        Id = id;
        AppointmentId = appointmentId;
        ExternalEmail = externalEmail;
        ExternalDisplayName = externalDisplayName;
    }

    /// <summary>
    /// Attaches a participant from a synced provider event who isn't a known team member (Story 2.1).
    /// </summary>
    public static Attendee External(Guid id, Guid appointmentId, string email, string? displayName) =>
        new(id, appointmentId, email, displayName);

    /// <summary>
    /// The label to show wherever this attendee is rendered — internal team members display by their
    /// <see cref="Person"/> record (caller resolves that), external participants fall back to whatever
    /// the provider gave us.
    /// </summary>
    public string ExternalDisplayLabel => ExternalDisplayName ?? ExternalEmail ?? string.Empty;
}
