using Domain;

namespace Api.Contracts;

public record AppointmentResponse(Guid Id, string Title, DateTimeOffset StartUtc, DateTimeOffset EndUtc, AvailabilityStatus Status);

public record CreateAppointmentRequest(string Title, DateTimeOffset StartUtc, DateTimeOffset EndUtc, IReadOnlyList<Guid> AttendeePersonIds);

public record AttendeeSummaryResponse(Guid PersonId, string Email);

public record AppointmentDetailResponse(
    Guid Id,
    string Title,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    AvailabilityStatus Status,
    IReadOnlyList<AttendeeSummaryResponse> Attendees);
