using System.Security.Claims;
using Application.Accounts;
using Domain;
using Microsoft.AspNetCore.Authorization;

namespace Api.Authorization;

/// <summary>
/// Loads <see cref="Person.Role"/> fresh from the database on every check (Story 1.1 Critical
/// Guardrail #6) — never trusts role claims baked into the auth cookie at sign-in time, so a
/// just-demoted admin loses access immediately rather than at next cookie refresh.
/// </summary>
public class AdminOnlyAuthorizationHandler(IPersonRepository personRepository)
    : AuthorizationHandler<AdminOnlyRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminOnlyRequirement requirement)
    {
        var idClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (idClaim is null || !Guid.TryParse(idClaim, out var personId))
        {
            return;
        }

        var person = await personRepository.GetByIdAsync(personId);
        if (person is not null && person.Role == PersonRole.Admin)
        {
            context.Succeed(requirement);
        }
    }
}
