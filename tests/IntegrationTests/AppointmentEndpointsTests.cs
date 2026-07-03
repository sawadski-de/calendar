using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domain;
using IntegrationTests.Infrastructure;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IntegrationTests;

[Collection("Postgres")]
public class AppointmentEndpointsTests(PostgresContainerFixture postgres)
{
    private async Task<(TestApiFactory Factory, HttpClient Client, Guid PersonId)> CreateAuthenticatedContextAsync()
    {
        var factory = new TestApiFactory(postgres.Container.GetConnectionString());
        var client = factory.CreateHttpsClient();

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestApiFactory.AdminEmail,
            password = TestApiFactory.AdminPassword,
        });
        login.EnsureSuccessStatusCode();

        var me = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        var personId = Guid.Parse(me.GetProperty("id").GetString()!);

        return (factory, client, personId);
    }

    private static async Task SeedAppointmentAsync(TestApiFactory factory, Guid personId, string title, DateTimeOffset startUtc, DateTimeOffset endUtc)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Appointments.Add(new Appointment(Guid.NewGuid(), personId, title, startUtc, endUtc));
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Returns_appointments_within_the_requested_range()
    {
        var (factory, client, personId) = await CreateAuthenticatedContextAsync();
        using var f = factory;
        using var c = client;

        var day = new DateTimeOffset(2026, 7, 6, 0, 0, 0, TimeSpan.Zero);
        await SeedAppointmentAsync(factory, personId, "Standup", day.AddHours(9), day.AddHours(9.5));
        await SeedAppointmentAsync(factory, personId, "Review", day.AddHours(14), day.AddHours(15));

        var response = await client.GetFromJsonAsync<JsonElement>(
            $"/api/appointments?from={Uri.EscapeDataString(day.ToString("O"))}&to={Uri.EscapeDataString(day.AddDays(1).ToString("O"))}");

        var titles = response.EnumerateArray().Select(e => e.GetProperty("title").GetString()).ToArray();
        Assert.Equal(new string?[] { "Standup", "Review" }, titles);
    }

    [Fact]
    public async Task Excludes_appointments_fully_outside_the_requested_range()
    {
        var (factory, client, personId) = await CreateAuthenticatedContextAsync();
        using var f = factory;
        using var c = client;

        var day = new DateTimeOffset(2026, 7, 6, 0, 0, 0, TimeSpan.Zero);
        await SeedAppointmentAsync(factory, personId, "NextWeek", day.AddDays(10), day.AddDays(10).AddHours(1));

        var response = await client.GetFromJsonAsync<JsonElement>(
            $"/api/appointments?from={Uri.EscapeDataString(day.ToString("O"))}&to={Uri.EscapeDataString(day.AddDays(1).ToString("O"))}");

        Assert.Empty(response.EnumerateArray());
    }

    [Fact]
    public async Task Includes_an_appointment_that_starts_before_the_range_but_overlaps_it()
    {
        var (factory, client, personId) = await CreateAuthenticatedContextAsync();
        using var f = factory;
        using var c = client;

        var rangeStart = new DateTimeOffset(2026, 7, 6, 0, 0, 0, TimeSpan.Zero);
        var rangeEnd = rangeStart.AddDays(1);
        // Starts the evening before the range, ends after midnight — inside the range.
        await SeedAppointmentAsync(factory, personId, "OvernightShift", rangeStart.AddHours(-2), rangeStart.AddHours(2));

        var response = await client.GetFromJsonAsync<JsonElement>(
            $"/api/appointments?from={Uri.EscapeDataString(rangeStart.ToString("O"))}&to={Uri.EscapeDataString(rangeEnd.ToString("O"))}");

        var titles = response.EnumerateArray().Select(e => e.GetProperty("title").GetString()).ToArray();
        Assert.Contains("OvernightShift", titles);
    }

    [Fact]
    public async Task Rejects_a_range_where_from_is_after_to()
    {
        var (factory, client, _) = await CreateAuthenticatedContextAsync();
        using var f = factory;
        using var c = client;

        var from = new DateTimeOffset(2026, 7, 10, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

        var response = await client.GetAsync(
            $"/api/appointments?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid-request", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Post_creates_a_native_appointment_with_computed_status()
    {
        var (factory, client, _) = await CreateAuthenticatedContextAsync();
        using var f = factory;
        using var c = client;

        var start = new DateTimeOffset(2026, 7, 6, 9, 0, 0, TimeSpan.Zero);
        var response = await client.PostAsJsonAsync("/api/appointments", new
        {
            title = "Solo focus block",
            startUtc = start,
            endUtc = start.AddHours(2),
            attendeePersonIds = Array.Empty<Guid>(),
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Solo focus block", body.GetProperty("title").GetString());
        // No attendees → always Unterbrechbar, regardless of the 2-hour duration.
        Assert.Equal(nameof(AvailabilityStatus.Unterbrechbar), body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Post_rejects_a_blank_title()
    {
        var (factory, client, _) = await CreateAuthenticatedContextAsync();
        using var f = factory;
        using var c = client;

        var start = DateTimeOffset.UtcNow;
        var response = await client.PostAsJsonAsync("/api/appointments", new
        {
            title = "   ",
            startUtc = start,
            endUtc = start.AddMinutes(30),
            attendeePersonIds = Array.Empty<Guid>(),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("title-required", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Post_rejects_an_end_time_at_or_before_the_start_time()
    {
        var (factory, client, _) = await CreateAuthenticatedContextAsync();
        using var f = factory;
        using var c = client;

        var start = DateTimeOffset.UtcNow;
        var response = await client.PostAsJsonAsync("/api/appointments", new
        {
            title = "Zero-length",
            startUtc = start,
            endUtc = start,
            attendeePersonIds = Array.Empty<Guid>(),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid-time-range", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Post_rejects_a_non_existent_attendee()
    {
        var (factory, client, _) = await CreateAuthenticatedContextAsync();
        using var f = factory;
        using var c = client;

        var start = DateTimeOffset.UtcNow;
        var response = await client.PostAsJsonAsync("/api/appointments", new
        {
            title = "With ghost attendee",
            startUtc = start,
            endUtc = start.AddMinutes(30),
            attendeePersonIds = new[] { Guid.NewGuid() },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("attendee-not-found", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Post_creates_an_appointment_with_valid_attendees_and_persists_them()
    {
        var (factory, client, _) = await CreateAuthenticatedContextAsync();
        using var f = factory;
        using var c = client;

        var createPerson = await client.PostAsJsonAsync("/api/admin/persons", new
        {
            email = $"attendee-{Guid.NewGuid():N}@test.local",
            password = "Member#12345",
            role = "Member",
        });
        createPerson.EnsureSuccessStatusCode();
        var attendeeId = (await createPerson.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var start = new DateTimeOffset(2026, 7, 6, 9, 0, 0, TimeSpan.Zero);
        var response = await client.PostAsJsonAsync("/api/appointments", new
        {
            title = "Kurzabstimmung",
            startUtc = start,
            endUtc = start.AddMinutes(30),
            attendeePersonIds = new[] { attendeeId },
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var appointmentId = body.GetProperty("id").GetGuid();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var attendeeCount = await dbContext.Attendees.CountAsync(a => a.AppointmentId == appointmentId && a.PersonId == attendeeId);
        Assert.Equal(1, attendeeCount);
    }

    [Fact]
    public async Task Get_response_includes_status()
    {
        var (factory, client, personId) = await CreateAuthenticatedContextAsync();
        using var f = factory;
        using var c = client;

        var day = new DateTimeOffset(2026, 7, 6, 0, 0, 0, TimeSpan.Zero);
        await SeedAppointmentAsync(factory, personId, "Standup", day.AddHours(9), day.AddHours(9.5));

        var response = await client.GetFromJsonAsync<JsonElement>(
            $"/api/appointments?from={Uri.EscapeDataString(day.ToString("O"))}&to={Uri.EscapeDataString(day.AddDays(1).ToString("O"))}");

        var first = response.EnumerateArray().First();
        Assert.Equal(nameof(AvailabilityStatus.Unterbrechbar), first.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Two_native_appointments_for_the_same_person_can_coexist_under_the_partial_unique_index()
    {
        // AD-7: Provider/ProviderEventId are both NULL for native appointments — the partial index
        // (WHERE provider_event_id IS NOT NULL) must not treat two NULLs as a duplicate.
        var (factory, _, personId) = await CreateAuthenticatedContextAsync();
        using var f = factory;

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTimeOffset.UtcNow;
        dbContext.Appointments.Add(new Appointment(Guid.NewGuid(), personId, "Native 1", now, now.AddHours(1)));
        dbContext.Appointments.Add(new Appointment(Guid.NewGuid(), personId, "Native 2", now.AddHours(2), now.AddHours(3)));

        await dbContext.SaveChangesAsync();

        var count = await dbContext.Appointments.CountAsync(a => a.PersonId == personId);
        Assert.Equal(2, count);
    }
}
