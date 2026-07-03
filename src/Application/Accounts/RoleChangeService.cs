using Domain;

namespace Application.Accounts;

public record RoleChangeResult(bool Succeeded, string? ErrorCode);

/// <summary>
/// Enforces the last-admin-protection rule (Story 1.1, AC 11) before persisting a role change.
/// Pure orchestration over <see cref="IPersonRepository"/> and <see cref="AdminPolicy"/> — no EF Core
/// or Identity dependency, so it is unit-testable in isolation (project-context Testing Rules).
/// </summary>
public class RoleChangeService(IPersonRepository personRepository)
{
    public Task<RoleChangeResult> ChangeRoleAsync(
        Guid personId,
        PersonRole newRole,
        CancellationToken cancellationToken = default) =>
        // The admin-count read and the role-change write must be atomic — otherwise two concurrent
        // demote requests can both read the same count before either commits, both pass the check,
        // and both write, leaving zero admins (the exact outcome this guardrail exists to prevent).
        personRepository.ExecuteAtomicallyAsync(async () =>
        {
            var person = await personRepository.GetByIdAsync(personId, cancellationToken);
            if (person is null)
            {
                return new RoleChangeResult(false, "person-not-found");
            }

            if (person.Role == PersonRole.Admin && newRole == PersonRole.Member)
            {
                var adminCount = await personRepository.CountAdminsAsync(cancellationToken);
                if (!AdminPolicy.CanDemoteLastAdmin(adminCount))
                {
                    return new RoleChangeResult(false, "last-admin-cannot-be-demoted");
                }
            }

            person.ChangeRole(newRole);
            await personRepository.SaveRoleChangeAsync(person, cancellationToken);
            return new RoleChangeResult(true, null);
        }, cancellationToken);
}
