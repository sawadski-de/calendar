using Domain;

namespace Application.Accounts;

public record ProvisionAccountResult(bool Succeeded, Guid? PersonId, IReadOnlyList<string> Errors);

/// <summary>
/// The only way a new account is created (Story 1.1, AC 2/10) — no self-signup endpoint exists.
/// Implemented in Infrastructure because it orchestrates ASP.NET Core Identity's UserManager.
/// </summary>
public interface IAccountProvisioningService
{
    Task<ProvisionAccountResult> ProvisionAsync(
        string email,
        string password,
        PersonRole role,
        CancellationToken cancellationToken = default);
}
