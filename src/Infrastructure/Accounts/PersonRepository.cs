using System.Data;
using Application.Accounts;
using Domain;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Accounts;

public class PersonRepository(ApplicationDbContext dbContext) : IPersonRepository
{
    public Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.People.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<int> CountAdminsAsync(CancellationToken cancellationToken = default) =>
        dbContext.People.CountAsync(p => p.Role == PersonRole.Admin, cancellationToken);

    public async Task AddAsync(Person person, CancellationToken cancellationToken = default)
    {
        dbContext.People.Add(person);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveRoleChangeAsync(Person person, CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);

    public async Task<T> ExecuteAtomicallyAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // A retried attempt must re-read the current DB state, not reuse entities the change
            // tracker still holds mutated (but rolled back) from the previous, failed attempt — that
            // stale in-memory state is exactly what let a "successful" retry silently persist nothing.
            dbContext.ChangeTracker.Clear();

            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var result = await operation();
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }
}
