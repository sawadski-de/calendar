namespace Domain;

public class Appointment
{
    public Guid Id { get; private set; }
    public Guid PersonId { get; private set; }
    public string Title { get; private set; }
    public DateTimeOffset StartUtc { get; private set; }
    public DateTimeOffset EndUtc { get; private set; }
    public string? Provider { get; private set; }
    public string? ProviderEventId { get; private set; }

    public Appointment(
        Guid id,
        Guid personId,
        string title,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        string? provider = null,
        string? providerEventId = null)
    {
        Id = id;
        PersonId = personId;
        Title = title;
        StartUtc = startUtc;
        EndUtc = endUtc;
        Provider = provider;
        ProviderEventId = providerEventId;
    }
}
