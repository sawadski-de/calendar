using Api.Contracts;
using Api.Errors;
using Application.Accounts;

namespace Api.Endpoints;

/// <summary>
/// The only way accounts are created or roles changed (Story 1.1, AC 2/4/11) — both gated behind the
/// Admin policy, which re-checks Person.Role fresh per request (see AdminOnlyAuthorizationHandler).
/// </summary>
public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin").RequireAuthorization("Admin");

        group.MapPost("/persons", async (CreatePersonRequest request, IAccountProvisioningService provisioning) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return ProblemResults.Problem(StatusCodes.Status400BadRequest, "invalid-request", "Email and password are required.");
            }

            var result = await provisioning.ProvisionAsync(request.Email, request.Password, request.Role);
            if (!result.Succeeded)
            {
                var code = result.Errors.Count > 0 ? result.Errors[0] : "provisioning-failed";
                var statusCode = code is "DuplicateUserName" or "DuplicateEmail"
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status400BadRequest;
                return ProblemResults.Problem(statusCode, code, "Could not create account.");
            }

            return Results.Created($"/api/admin/persons/{result.PersonId}", new { id = result.PersonId });
        });

        group.MapPut("/persons/{id:guid}/role", async (Guid id, ChangeRoleRequest request, RoleChangeService roleChangeService) =>
        {
            var result = await roleChangeService.ChangeRoleAsync(id, request.Role);
            if (!result.Succeeded)
            {
                var statusCode = result.ErrorCode == "person-not-found"
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status409Conflict;
                return ProblemResults.Problem(statusCode, result.ErrorCode!, "Could not change role.");
            }

            return Results.NoContent();
        });
    }
}
