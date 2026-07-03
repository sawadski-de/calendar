using Domain;

namespace Application.Accounts;

public interface IPersonRepository
{
    Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<int> CountAdminsAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Person person, CancellationToken cancellationToken = default);

    Task SaveRoleChangeAsync(Person person, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="operation"/> inside a Serializable database transaction (with automatic
    /// retry on a detected write-skew conflict). <see cref="RoleChangeService"/> uses this so the
    /// last-admin-protection read-then-write is atomic — without it, two concurrent demote requests
    /// can each read the same admin count before either commits, defeating the guardrail entirely.
    /// </summary>
    Task<T> ExecuteAtomicallyAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
}
