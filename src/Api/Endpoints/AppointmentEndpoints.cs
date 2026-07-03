using System.Security.Claims;
using Api.Contracts;
using Api.Errors;
using Application.Accounts;
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

            var response = appointments.Select(a => new AppointmentResponse(a.Id, a.Title, a.StartUtc, a.EndUtc, a.Status));
            return Results.Ok(response);
        }).RequireAuthorization();

        app.MapGet("/api/appointments/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            IAppointmentViewService appointmentViewService,
            IPersonRepository personRepository,
            CancellationToken cancellationToken) =>
        {
            var personId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var appointment = await appointmentViewService.GetOwnAppointmentByIdAsync(personId, id, cancellationToken);
            if (appointment is null)
            {
                return ProblemResults.Problem(StatusCodes.Status404NotFound, "appointment-not-found", "Appointment not found.");
            }

            var attendeePersonIds = appointment.Attendees.Select(a => a.PersonId).ToHashSet();
            var roster = await personRepository.GetAllAsync(cancellationToken);
            var attendees = roster
                .Where(p => attendeePersonIds.Contains(p.Id))
                .Select(p => new AttendeeSummaryResponse(p.Id, p.Email))
                .ToList();

            var response = new AppointmentDetailResponse(
                appointment.Id, appointment.Title, appointment.StartUtc, appointment.EndUtc, appointment.Status, attendees);
            return Results.Ok(response);
        }).RequireAuthorization();

        app.MapPost("/api/appointments", async (
            CreateAppointmentRequest request,
            ClaimsPrincipal user,
            IAppointmentCreationService appointmentCreationService,
            CancellationToken cancellationToken) =>
        {
            var personId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await appointmentCreationService.CreateNativeAppointmentAsync(
                personId,
                request.Title,
                request.StartUtc,
                request.EndUtc,
                request.AttendeePersonIds,
                cancellationToken);

            if (!result.Succeeded)
            {
                return ProblemResults.Problem(StatusCodes.Status400BadRequest, result.ErrorCode!, "Could not create appointment.");
            }

            var appointment = result.Appointment!;
            var response = new AppointmentResponse(appointment.Id, appointment.Title, appointment.StartUtc, appointment.EndUtc, appointment.Status);
            return Results.Created($"/api/appointments/{appointment.Id}", response);
        }).RequireAuthorization();
    }
}
