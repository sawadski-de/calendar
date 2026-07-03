namespace Api.Contracts;

public record AppointmentResponse(Guid Id, string Title, DateTimeOffset StartUtc, DateTimeOffset EndUtc);
