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

    /// <summary>
    /// Stored for full internal access (FR-8) but not yet surfaced in any UI — the detail popover
    /// deliberately omits it until FR-15 (map display) is built (Story 1.4 Dev Notes). Always
    /// <c>null</c> for native appointments today, since there is no location input in the create form
    /// yet; populated for synced events (Story 2.1) from the provider's location field.
    /// </summary>
    public string? Location { get; private set; }

    public Appointment(
        Guid id,
        Guid personId,
        string title,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        string? provider = null,
        string? providerEventId = null,
        bool isAllDay = false,
        string? location = null)
    {
        Id = id;
        PersonId = personId;
        Title = title;
        StartUtc = startUtc;
        EndUtc = endUtc;
        Provider = provider;
        ProviderEventId = providerEventId;
        IsAllDay = isAllDay;
        Location = location;
    }

    /// <summary>
    /// Attaches an internal attendee. Called once per selected team member during native creation
    /// (Story 1.3) — not part of the constructor, since attendee count must be final before
    /// <see cref="AssignStatus"/> runs and EF Core's collection-navigation binding works via the
    /// backing field, not a constructor parameter.
    /// </summary>
    public void AddAttendee(Guid personId) => _attendees.Add(new Attendee(Guid.NewGuid(), Id, personId));

    /// <summary>
    /// Attaches a participant carried over from a synced provider event (Story 2.1) whose email could
    /// not be matched to a team member — the sync service tries the roster first (via
    /// <see cref="AddAttendee"/>) and only falls back to this external-attendee path when no match is
    /// found.
    /// </summary>
    public void AddExternalAttendee(string email, string? displayName) =>
        _attendees.Add(Attendee.External(Guid.NewGuid(), Id, email, displayName));

    /// <summary>
    /// Stores the availability status computed by <see cref="StatusHeuristicService.Compute"/> (AD-4).
    /// The only way <see cref="Status"/> is ever set after construction — called exactly once, after
    /// all attendees are attached, never recomputed on read.
    /// </summary>
    public void AssignStatus(AvailabilityStatus status) => Status = status;
}
