using System.Security.Claims;
using Api.Contracts;
using Api.Errors;
using Application.Appointments;

namespace Api.Endpoints;

public static class AppointmentEndpoints
{
    public static void MapAppointmentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/appointments", async (
            DateTimeOffset from,
            DateTimeOffset to,
            ClaimsPrincipal user,
            IAppointmentViewService appointmentViewService,
            CancellationToken cancellationToken) =>
        {
            if (from > to)
            {
                return ProblemResults.Problem(StatusCodes.Status400BadRequest, "invalid-request", "'from' must not be after 'to'.");
            }

            var personId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var appointments = await appointmentViewService.GetOwnAppointmentsAsync(personId, from, to, cancellationToken);

            var response = appointments.Select(a => new AppointmentResponse(a.Id, a.Title, a.StartUtc, a.EndUtc));
            return Results.Ok(response);
        }).RequireAuthorization();
    }
}
