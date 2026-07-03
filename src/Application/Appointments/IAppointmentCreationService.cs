using Domain;

namespace Application.Appointments;

public record AppointmentCreationResult(bool Succeeded, Appointment? Appointment, string? ErrorCode);

/// <summary>
/// The only write path for native appointments (Story 1.3). Kept separate from
/// <see cref="IAppointmentViewService"/>, which AD-3 scopes to reads only.
/// </summary>
public interface IAppointmentCreationService
{
    Task<AppointmentCreationResult> CreateNativeAppointmentAsync(
        Guid ownerId,
        string title,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        IReadOnlyCollection<Guid> attendeePersonIds,
        CancellationToken cancellationToken = default);
}
