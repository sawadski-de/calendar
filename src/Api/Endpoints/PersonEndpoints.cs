using Api.Contracts;
using Application.Accounts;

namespace Api.Endpoints;

/// <summary>
/// The team roster, available to any authenticated member (not Admin-gated like <c>/api/admin/*</c>)
/// so the attendee picker (Story 1.3) can list everyone.
/// </summary>
public static class PersonEndpoints
{
    public static void MapPersonEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/persons", async (IPersonRepository personRepository, CancellationToken cancellationToken) =>
        {
            var people = await personRepository.GetAllAsync(cancellationToken);
            var response = people.Select(p => new PersonResponse(p.Id, p.Email));
            return Results.Ok(response);
        }).RequireAuthorization();
    }
}
