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
