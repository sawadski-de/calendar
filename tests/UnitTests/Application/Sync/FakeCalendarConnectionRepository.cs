using Application.Sync;
using Domain;

namespace UnitTests.Application.Sync;

public class FakeCalendarConnectionRepository : ICalendarConnectionRepository
{
    private readonly Dictionary<Guid, CalendarConnection> _connections = new();

    public int UpsertCallCount { get; private set; }

    /// <summary>Inserts directly, bypassing <see cref="UpsertAsync"/> — for test setup only, so
    /// <see cref="UpsertCallCount"/> stays a true count of calls the code under test made.</summary>
    public void Seed(CalendarConnection connection) => _connections[connection.Id] = connection;

    public Task<CalendarConnection?> GetAsync(Guid personId, string provider, CancellationToken cancellationToken = default) =>
        Task.FromResult(_connections.Values.FirstOrDefault(c => c.PersonId == personId && c.Provider == provider));

    public Task<IReadOnlyList<CalendarConnection>> GetByPersonAsync(Guid personId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CalendarConnection>>(_connections.Values.Where(c => c.PersonId == personId).ToList());

    public Task<IReadOnlyList<CalendarConnection>> GetAllActiveAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CalendarConnection>>(_connections.Values.ToList());

    public Task<IReadOnlyList<CalendarConnection>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CalendarConnection>>(_connections.Values.ToList());

    public Task UpsertAsync(CalendarConnection connection, CancellationToken cancellationToken = default)
    {
        UpsertCallCount++;
        _connections[connection.Id] = connection;
        return Task.CompletedTask;
    }

    public Task DisconnectAndRemoveAppointmentsAsync(
        CalendarConnection connection,
        IReadOnlyList<Guid> appointmentIdsToDelete,
        CancellationToken cancellationToken = default)
    {
        _connections[connection.Id] = connection;
        return Task.CompletedTask;
    }
}
