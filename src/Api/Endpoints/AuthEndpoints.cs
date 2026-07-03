using System.Security.Claims;
using Api.Contracts;
using Api.Errors;
using Application.Accounts;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Api.Endpoints;

/// <summary>
/// Hand-rolled login/logout only (Story 1.1 Critical Guardrail #1) — MapIdentityApi&lt;TUser&gt;()
/// is deliberately not used because it would also expose /register and violate "no self-signup".
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (LoginRequest request, SignInManager<ApplicationUser> signInManager) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return ProblemResults.Problem(StatusCodes.Status401Unauthorized, "invalid-credentials", "Login failed.");
            }

            var result = await signInManager.PasswordSignInAsync(
                request.Email,
                request.Password,
                isPersistent: false,
                lockoutOnFailure: false);

            return result.Succeeded
                ? Results.Ok()
                : ProblemResults.Problem(StatusCodes.Status401Unauthorized, "invalid-credentials", "Login failed.");
        });

        app.MapPost("/api/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.Ok();
        }).RequireAuthorization();

        // Session-check for the Angular route guard (AC 3): the auth cookie is HttpOnly and
        // therefore invisible to client JS, so the SPA has no other way to know it is logged in.
        // Role is read fresh from the DB (Story 2.3) — never trust a cached claim for a role-gated
        // nav decision, same rule AdminOnlyAuthorizationHandler already follows for the real 403 check
        // this only ever gates a convenience nav link, never replaces server-side enforcement (AD-17).
        app.MapGet("/api/auth/me", async (ClaimsPrincipal user, IPersonRepository personRepository, CancellationToken cancellationToken) =>
        {
            var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = user.FindFirstValue(ClaimTypes.Email);
            var person = await personRepository.GetByIdAsync(Guid.Parse(id!), cancellationToken);
            return Results.Ok(new { id, email, role = person?.Role.ToString() });
        }).RequireAuthorization();
    }
}
