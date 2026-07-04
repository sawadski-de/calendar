using Domain;

namespace Application.Appointments;

/// <summary>
/// One appointment slot for a colleague's calendar column — status-only by construction, no
/// <c>Title</c> field exists on this type at all (AC 3, Story 3.1).
/// </summary>
public sealed record ViewerAppointmentSlot(Guid PersonId, Guid Id, DateTimeOffset StartUtc, DateTimeOffset EndUtc, AvailabilityStatus Status);

/// <summary>
/// Result of a single-appointment read through <see cref="IAppointmentViewService.GetForViewerByIdAsync"/>.
/// <c>IsFullDetail</c> decides whether <c>FullAppointment</c> is populated (owner or listed attendee,
/// FR-9) or the caller only gets <c>Status</c> (Story 3.1, AD-3).
/// </summary>
public sealed record AppointmentViewResult(Guid Id, bool IsFullDetail, Appointment? FullAppointment, AvailabilityStatus Status);
