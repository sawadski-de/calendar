using System.Security.Claims;
using Application.Accounts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Infrastructure.Identity;

/// <summary>
/// Bakes <see cref="Domain.Person.Role"/> into the auth cookie at sign-in time as <see cref="RoleClaimType"/>,
/// so <c>/api/auth/me</c> can answer the Angular route guard's nav-gating question ("show the Admin
/// link?") without a DB round-trip on every SPA route navigation (code review finding, Story 2.3
/// follow-up). This claim is display-only: <see cref="Api.Authorization.AdminOnlyAuthorizationHandler"/>
/// still re-reads <see cref="Domain.Person.Role"/> fresh from the DB on every admin-endpoint request
/// and never trusts this claim (Story 1.1 Critical Guardrail #6) — so a role change taking effect in
/// the nav only after the next login is an accepted UX tradeoff, not a security gap.
/// </summary>
public class ApplicationUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    IOptions<IdentityOptions> optionsAccessor,
    IPersonRepository personRepository)
    : UserClaimsPrincipalFactory<ApplicationUser>(userManager, optionsAccessor)
{
    public const string RoleClaimType = "person_role";

    public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
    {
        var principal = await base.CreateAsync(user);

        // Provisioning creates the ApplicationUser and Person together in one transaction (see
        // IdentityAccountProvisioningService), so by the time anyone can log in, the Person exists.
        var person = await personRepository.GetByIdAsync(user.Id);
        if (person is not null)
        {
            ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim(RoleClaimType, person.Role.ToString()));
        }

        return principal;
    }
}
