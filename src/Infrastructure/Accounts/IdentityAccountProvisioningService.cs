using Application.Accounts;
using Domain;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Accounts;

/// <summary>
/// Creates the <see cref="ApplicationUser"/> (Identity) and <see cref="Person"/> (Domain) pair
/// atomically, sharing one Id, inside a single database transaction — if either half fails, both
/// are rolled back so an orphaned auth-only or person-only account can never exist.
/// </summary>
public class IdentityAccountProvisioningService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    IPersonRepository personRepository) : IAccountProvisioningService
{
    public Task<ProvisionAccountResult> ProvisionAsync(
        string email,
        string password,
        PersonRole role,
        CancellationToken cancellationToken = default)
    {
        // The DbContext is configured with EnableRetryOnFailure (for RoleChangeService's Serializable
        // transaction), which requires every manual transaction — this one included — to run through
        // the execution strategy rather than a bare BeginTransactionAsync; otherwise EF Core throws.
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            var id = Guid.NewGuid();

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var user = new ApplicationUser
            {
                Id = id,
                UserName = email,
                Email = email,
                EmailConfirmed = true,
            };

            try
            {
                var identityResult = await userManager.CreateAsync(user, password);
                if (!identityResult.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    var errors = identityResult.Errors.Select(e => e.Code).ToArray();
                    return new ProvisionAccountResult(false, null, errors);
                }

                var person = new Person(id, email, role);
                await personRepository.AddAsync(person, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return new ProvisionAccountResult(true, id, Array.Empty<string>());
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Two concurrent requests for the same email can both pass Identity's own uniqueness
                // check (which queries before either transaction commits) and only collide at the
                // database's unique index — map that race to the same code Identity uses non-concurrently
                // rather than letting a raw DbUpdateException surface as a generic 500.
                await transaction.RollbackAsync(cancellationToken);
                return new ProvisionAccountResult(false, null, new[] { "DuplicateEmail" });
            }
        });
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
