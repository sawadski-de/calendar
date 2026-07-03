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
    public async Task Post_with_a_missing_attendeePersonIds_field_creates_the_appointment_instead_of_500ing()
    {
        // Regression: System.Text.Json binds a missing JSON property to null regardless of the
        // record's non-nullable C# type — attendeePersonIds must be treated as "no attendees", not crash.
        var (factory, client, _) = await CreateAuthenticatedContextAsync();
        using var f = factory;
        using var c = client;

        var start = new DateTimeOffset(2026, 7, 6, 9, 0, 0, TimeSpan.Zero);
        var response = await client.PostAsJsonAsync("/api/appointments", new
        {
            title = "No attendee field at all",
            startUtc = start,
            endUtc = start.AddMinutes(30),
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
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
    public async Task A_row_relying_on_the_status_column_default_reads_back_as_a_valid_enum_value()
    {
        // Regression: the AddAppointmentStatusAndAttendees migration's ADD COLUMN must backfill
        // pre-existing rows with a valid AvailabilityStatus member (not an empty string, which the
        // HasConversion<string>() enum mapping cannot parse on the next read).
        var (factory, client, personId) = await CreateAuthenticatedContextAsync();
        using var f = factory;
        using var c = client;

        var appointmentId = Guid.NewGuid();
        var day = new DateTimeOffset(2026, 7, 6, 9, 0, 0, TimeSpan.Zero);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            // Omits is_all_day/status entirely so the row relies on the column defaults, simulating a
            // pre-existing row backfilled by the migration rather than written through EF's mapping.
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO appointments (id, person_id, title, start_utc, end_utc) VALUES ({appointmentId}, {personId}, {"Legacy"}, {day}, {day.AddMinutes(30)})");
        }

        var response = await client.GetFromJsonAsync<JsonElement>(
            $"/api/appointments?from={Uri.EscapeDataString(day.ToString("O"))}&to={Uri.EscapeDataString(day.AddDays(1).ToString("O"))}");

        var entry = response.EnumerateArray().Single();
        Assert.Equal(nameof(AvailabilityStatus.Unterbrechbar), entry.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Post_deduplicates_a_repeated_attendee_id_instead_of_erroring()
    {
        var (factory, client, _) = await CreateAuthenticatedContextAsync();
        using var f = factory;
        using var c = client;

        var createPerson = await client.PostAsJsonAsync("/api/admin/persons", new
        {
            email = $"dup-{Guid.NewGuid():N}@test.local",
            password = "Member#12345",
            role = "Member",
        });
        createPerson.EnsureSuccessStatusCode();
        var attendeeId = (await createPerson.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var start = new DateTimeOffset(2026, 7, 6, 9, 0, 0, TimeSpan.Zero);
        var response = await client.PostAsJsonAsync("/api/appointments", new
        {
            title = "Repeated attendee",
            startUtc = start,
            endUtc = start.AddMinutes(30),
            attendeePersonIds = new[] { attendeeId, attendeeId },
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var appointmentId = body.GetProperty("id").GetGuid();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var attendeeRowCount = await dbContext.Attendees.CountAsync(a => a.AppointmentId == appointmentId);
        Assert.Equal(1, attendeeRowCount);
    }

    [Fact]
    public async Task Get_by_id_returns_full_detail_including_attendee_emails()
    {
        var (factory, client, _) = await CreateAuthenticatedContextAsync();
        using var f = factory;
        using var c = client;

        var createAttendee = await client.PostAsJsonAsync("/api/admin/persons", new
        {
            email = $"detail-{Guid.NewGuid():N}@test.local",
            password = "Member#12345",
            role = "Member",
        });
        createAttendee.EnsureSuccessStatusCode();
        var attendeeId = (await createAttendee.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var attendeeEmail = (await client.GetFromJsonAsync<JsonElement>("/api/persons"))
            .EnumerateArray()
            .First(p => p.GetProperty("id").GetGuid() == attendeeId)
            .GetProperty("email")
            .GetString();

        var start = new DateTimeOffset(2026, 7, 6, 9, 0, 0, TimeSpan.Zero);
        var createResponse = await client.PostAsJsonAsync("/api/appointments", new
        {
            title = "Detail-Test",
            startUtc = start,
            endUtc = start.AddMinutes(30),
            attendeePersonIds = new[] { attendeeId },
        });
        createResponse.EnsureSuccessStatusCode();
        var appointmentId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/appointments/{appointmentId}");

        Assert.Equal("Detail-Test", detail.GetProperty("title").GetString());
        Assert.Equal(nameof(AvailabilityStatus.Unterbrechbar), detail.GetProperty("status").GetString());
        var attendees = detail.GetProperty("attendees").EnumerateArray().ToArray();
        Assert.Single(attendees);
        Assert.Equal(attendeeEmail, attendees[0].GetProperty("email").GetString());
    }

    [Fact]
    public async Task Get_by_id_returns_404_for_a_non_existent_appointment()
    {
        var (factory, client, _) = await CreateAuthenticatedContextAsync();
        using var f = factory;
        using var c = client;

        var response = await client.GetAsync($"/api/appointments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("appointment-not-found", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_by_id_returns_404_for_another_persons_appointment()
    {
        // Same code/status as "doesn't exist" (Get_by_id_returns_404_for_a_non_existent_appointment) —
        // no existence leak for an appointment the caller doesn't own.
        var (factory, ownerClient, ownerId) = await CreateAuthenticatedContextAsync();
        using var f = factory;
        using var oc = ownerClient;

        var start = DateTimeOffset.UtcNow;
        await SeedAppointmentAsync(factory, ownerId, "Owner's appointment", start, start.AddMinutes(30));

        Guid appointmentId;
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            appointmentId = await dbContext.Appointments.Where(a => a.PersonId == ownerId).Select(a => a.Id).SingleAsync();
        }

        var otherEmail = $"other-{Guid.NewGuid():N}@test.local";
        var createOther = await ownerClient.PostAsJsonAsync("/api/admin/persons", new
        {
            email = otherEmail,
            password = "Member#12345",
            role = "Member",
        });
        createOther.EnsureSuccessStatusCode();

        using var otherClient = factory.CreateHttpsClient();
        var otherLogin = await otherClient.PostAsJsonAsync("/api/auth/login", new { email = otherEmail, password = "Member#12345" });
        otherLogin.EnsureSuccessStatusCode();

        var response = await otherClient.GetAsync($"/api/appointments/{appointmentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("appointment-not-found", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_by_id_response_has_no_location_property()
    {
        var (factory, client, personId) = await CreateAuthenticatedContextAsync();
        using var f = factory;
        using var c = client;

        var start = DateTimeOffset.UtcNow;
        await SeedAppointmentAsync(factory, personId, "No location", start, start.AddMinutes(30));

        Guid appointmentId;
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            appointmentId = await dbContext.Appointments.Where(a => a.PersonId == personId).Select(a => a.Id).SingleAsync();
        }

        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/appointments/{appointmentId}");
        var propertyNames = detail.EnumerateObject().Select(p => p.Name).ToArray();

        Assert.DoesNotContain(propertyNames, name => name.Contains("location", StringComparison.OrdinalIgnoreCase));
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
