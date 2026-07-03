using Application.Sync;
using Domain;

namespace UnitTests.Application.Sync;

public class FakeCalendarConnectionRepository : ICalendarConnectionRepository
{
    private readonly Dictionary<Guid, CalendarConnection> _connections = new();

    public int UpsertCallCount { get; private set; }

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
}
