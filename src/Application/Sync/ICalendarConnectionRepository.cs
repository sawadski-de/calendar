using Domain;

namespace Application.Sync;

/// <summary>
/// Persistence for <see cref="CalendarConnection"/>. Kept separate from any read-only view service
/// (mirrors the <c>IAppointmentRepository</c>/<c>IAppointmentViewService</c> split, AD-3's write/read
/// separation applied here too) — the Api only ever calls <see cref="UpsertAsync"/> once, at the
/// initial OAuth handshake; the Worker calls it every sync cycle to persist refreshed tokens and
/// sync-status fields (AD-8, AD-16).
/// </summary>
public interface ICalendarConnectionRepository
{
    Task<CalendarConnection?> GetAsync(Guid personId, string provider, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CalendarConnection>> GetByPersonAsync(Guid personId, CancellationToken cancellationToken = default);

    /// <summary>
    /// All connections belonging to an active person — a deactivated person's connections must not be
    /// polled (AD-12). <see cref="Person"/> has no <c>IsActive</c> field yet (Epic 5 introduces it); until
    /// then this returns every connection, since there is nothing to filter on.
    /// </summary>
    Task<IReadOnlyList<CalendarConnection>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Every connection for every person, regardless of <c>Person.IsActive</c> — for the Admin sync
    /// overview (Story 2.3), which must still show a deactivated person's last-known state (AD-12's
    /// "data is retained" spirit). Deliberately separate from <see cref="GetAllActiveAsync"/>: that one
    /// serves the Worker's polling loop and must keep filtering out inactive people once Epic 5 lands.
    /// </summary>
    Task<IReadOnlyList<CalendarConnection>> GetAllAsync(CancellationToken cancellationToken = default);

    Task UpsertAsync(CalendarConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// User-initiated "Verbindung trennen" — clears the connection's tokens/state
    /// (<see cref="CalendarConnection.Disconnect"/>) and removes every appointment
    /// <paramref name="appointmentIdsToDelete"/> names, atomically. Deliberately one call, not
    /// "delete appointments then <see cref="UpsertAsync"/>" as two separate writes — a crash between
    /// the two would leave <see cref="CalendarConnection.IsConnected"/> reading true for an account
    /// whose imported data was already wiped (code review finding).
    /// </summary>
    Task DisconnectAndRemoveAppointmentsAsync(
        CalendarConnection connection,
        IReadOnlyList<Guid> appointmentIdsToDelete,
        CancellationToken cancellationToken = default);
}
