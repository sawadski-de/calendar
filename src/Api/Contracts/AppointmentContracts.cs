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

/// <summary>
/// <c>Title</c>/<c>StartUtc</c>/<c>EndUtc</c>/<c>Attendees</c> are <c>null</c> — not merely unrendered
/// — whenever <c>IsFullDetail</c> is <c>false</c> (Story 3.1, FR-9): the viewer is neither the owner
/// nor a listed attendee, so only <c>Status</c> is meaningful.
/// </summary>
public record AppointmentDetailResponse(
    Guid Id,
    bool IsFullDetail,
    string? Title,
    DateTimeOffset? StartUtc,
    DateTimeOffset? EndUtc,
    AvailabilityStatus Status,
    IReadOnlyList<AttendeeSummaryResponse>? Attendees);

/// <summary>
/// One colleague calendar-column slot — deliberately has no <c>Title</c> field at all, regardless of
/// attendee status (Story 3.1, AC 3): the bulk multi-person view always renders status blocks only.
/// </summary>
public record ColleagueAppointmentSlotResponse(Guid Id, DateTimeOffset StartUtc, DateTimeOffset EndUtc, AvailabilityStatus Status);
