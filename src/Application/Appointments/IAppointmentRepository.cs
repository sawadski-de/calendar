using Domain;

namespace Application.Appointments;

/// <summary>
/// The write side of appointment persistence — separate from the read-only
/// <see cref="IAppointmentViewService"/> (AD-3 scopes that interface to reads only).
/// </summary>
public interface IAppointmentRepository
{
    Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every currently-stored synced appointment for one connection, keyed by
    /// <see cref="Appointment.ProviderEventId"/> — the diff base a sync cycle compares the provider's
    /// fresh snapshot against (AD-7).
    /// </summary>
    Task<IReadOnlyDictionary<string, Appointment>> GetSyncedAppointmentsAsync(
        Guid personId,
        string provider,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies one sync cycle's diff atomically: inserts new/changed appointments (a changed
    /// appointment arrives here as a fresh <see cref="Appointment"/> with a new <c>Id</c> — the
    /// immutable Domain model has no in-place field setters for a re-imported event, so "update"
    /// is modeled as replace) and removes appointments whose provider key vanished from the latest
    /// snapshot (AD-7).
    /// </summary>
    Task ApplySyncResultAsync(
        IReadOnlyList<Appointment> toInsert,
        IReadOnlyList<Guid> idsToDelete,
        CancellationToken cancellationToken = default);
}
