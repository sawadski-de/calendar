using Application.Accounts;
using Domain;

namespace UnitTests.Application;

/// <summary>
/// Hand-rolled in-memory fake — no EF Core/DB dependency, so <see cref="Accounts.RoleChangeServiceTests"/>
/// stays a true unit test per project-context Testing Rules.
/// </summary>
public class FakePersonRepository : IPersonRepository
{
    private readonly Dictionary<Guid, Person> _people = new();

    public FakePersonRepository(params Person[] seed)
    {
        foreach (var person in seed)
        {
            _people[person.Id] = person;
        }
    }

    public Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_people.GetValueOrDefault(id));

    public Task<int> CountAdminsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_people.Values.Count(p => p.Role == PersonRole.Admin));

    public Task AddAsync(Person person, CancellationToken cancellationToken = default)
    {
        _people[person.Id] = person;
        return Task.CompletedTask;
    }

    public Task SaveRoleChangeAsync(Person person, CancellationToken cancellationToken = default)
    {
        _people[person.Id] = person;
        return Task.CompletedTask;
    }

    // No real transaction semantics needed here — RoleChangeServiceTests exercises the business
    // rule in isolation, not concurrency, which is covered separately by the Testcontainers-backed
    // integration tests against a real Postgres instance.
    public Task<T> ExecuteAtomicallyAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default) =>
        operation();
}
