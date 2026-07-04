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
            var result = await appointmentViewService.GetForViewerByIdAsync(personId, id, cancellationToken);
            if (result is null)
            {
                return ProblemResults.Problem(StatusCodes.Status404NotFound, "appointment-not-found", "Appointment not found.");
            }

            if (!result.IsFullDetail)
            {
                // FR-9 status-only default: the requester is neither owner nor a listed attendee.
                return Results.Ok(new AppointmentDetailResponse(result.Id, false, null, null, null, result.Status, null));
            }

            var appointment = result.FullAppointment!;
            var internalAttendeeIds = appointment.Attendees
                .Where(a => a.PersonId.HasValue)
                .Select(a => a.PersonId!.Value)
                .ToHashSet();
            var roster = await personRepository.GetAllAsync(cancellationToken);
            var emailByPersonId = roster
                .Where(p => internalAttendeeIds.Contains(p.Id))
                .ToDictionary(p => p.Id, p => p.Email);
            var attendees = appointment.Attendees
                .Select(a => a.PersonId.HasValue
                    ? new AttendeeSummaryResponse(a.PersonId, emailByPersonId.GetValueOrDefault(a.PersonId.Value, string.Empty))
                    : new AttendeeSummaryResponse(null, a.ExternalDisplayLabel))
                .ToList();

            var response = new AppointmentDetailResponse(
                appointment.Id, true, appointment.Title, appointment.StartUtc, appointment.EndUtc, appointment.Status, attendees);
            return Results.Ok(response);
        }).RequireAuthorization();

        app.MapGet("/api/appointments/colleagues", async (
            Guid[] personIds,
            DateTimeOffset from,
            DateTimeOffset to,
            IAppointmentViewService appointmentViewService,
            CancellationToken cancellationToken) =>
        {
            if (from > to)
            {
                return ProblemResults.Problem(StatusCodes.Status400BadRequest, "invalid-request", "'from' must not be after 'to'.");
            }

            var distinctIds = personIds.Distinct().ToList();
            var slotsByPerson = await appointmentViewService.GetForViewersAsync(distinctIds, from, to, cancellationToken);

            var response = distinctIds.ToDictionary(
                id => id,
                id => (IReadOnlyList<ColleagueAppointmentSlotResponse>)(slotsByPerson.TryGetValue(id, out var slots)
                    ? slots.Select(s => new ColleagueAppointmentSlotResponse(s.Id, s.StartUtc, s.EndUtc, s.Status)).ToList()
                    : []));

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
