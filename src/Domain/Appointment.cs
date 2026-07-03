namespace Domain;

public class Appointment
{
    private readonly List<Attendee> _attendees = [];

    public Guid Id { get; private set; }
    public Guid PersonId { get; private set; }
    public string Title { get; private set; }
    public DateTimeOffset StartUtc { get; private set; }
    public DateTimeOffset EndUtc { get; private set; }
    public string? Provider { get; private set; }
    public string? ProviderEventId { get; private set; }
    public bool IsAllDay { get; private set; }
    public AvailabilityStatus Status { get; private set; }
    public IReadOnlyCollection<Attendee> Attendees => _attendees;

    public Appointment(
        Guid id,
        Guid personId,
        string title,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        string? provider = null,
        string? providerEventId = null,
        bool isAllDay = false)
    {
        Id = id;
        PersonId = personId;
        Title = title;
        StartUtc = startUtc;
        EndUtc = endUtc;
        Provider = provider;
        ProviderEventId = providerEventId;
        IsAllDay = isAllDay;
    }

    /// <summary>
    /// Attaches an internal attendee. Called once per selected team member during native creation
    /// (Story 1.3) — not part of the constructor, since attendee count must be final before
    /// <see cref="AssignStatus"/> runs and EF Core's collection-navigation binding works via the
    /// backing field, not a constructor parameter.
    /// </summary>
    public void AddAttendee(Guid personId) => _attendees.Add(new Attendee(Guid.NewGuid(), Id, personId));

    /// <summary>
    /// Stores the availability status computed by <see cref="StatusHeuristicService.Compute"/> (AD-4).
    /// The only way <see cref="Status"/> is ever set after construction — called exactly once, after
    /// all attendees are attached, never recomputed on read.
    /// </summary>
    public void AssignStatus(AvailabilityStatus status) => Status = status;
}
