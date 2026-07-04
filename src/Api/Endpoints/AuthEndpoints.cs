using System.Security.Claims;
using Api.Contracts;
using Api.Errors;
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
        // The guard calls this on every route navigation, so the role comes from the claim baked in
        // at sign-in (ApplicationUserClaimsPrincipalFactory) rather than a DB query per navigation
        // (code review finding). This only ever gates a convenience nav link — server-side
        // enforcement for admin-only endpoints still reads Person.Role fresh from the DB on every
        // request via AdminOnlyAuthorizationHandler and never trusts this claim (AD-17).
        app.MapGet("/api/auth/me", (ClaimsPrincipal user) =>
        {
            var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = user.FindFirstValue(ClaimTypes.Email);
            var role = user.FindFirstValue(ApplicationUserClaimsPrincipalFactory.RoleClaimType);
            return Results.Ok(new { id, email, role });
        }).RequireAuthorization();
    }
}
