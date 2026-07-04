using Domain;

namespace Api.Contracts;

public record AppointmentResponse(Guid Id, string Title, DateTimeOffset StartUtc, DateTimeOffset EndUtc, AvailabilityStatus Status);

public record CreateAppointmentRequest(string Title, DateTimeOffset StartUtc, DateTimeOffset EndUtc, IReadOnlyList<Guid> AttendeePersonIds);

/// <summary>
/// <c>PersonId</c> is <c>null</c> for an external participant carried over from a synced provider
/// event (Story 2.1) — <c>Email</c> still holds a display value in both cases (the team member's
/// email, or the external attendee's email/display name).
/// </summary>
public record AttendeeSummaryResponse(Guid? PersonId, string Email);

public record AppointmentDetailResponse(
    Guid Id,
    string Title,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    AvailabilityStatus Status,
    IReadOnlyList<AttendeeSummaryResponse> Attendees);
