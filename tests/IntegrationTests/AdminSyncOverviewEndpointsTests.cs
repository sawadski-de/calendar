using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Sync;
using Domain;
using IntegrationTests.Infrastructure;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IntegrationTests;

[Collection("Postgres")]
public class AdminSyncOverviewEndpointsTests(PostgresContainerFixture postgres)
{
    private TestApiFactory CreateFactory() => new(postgres.Container.GetConnectionString());

    private static async Task<HttpClient> LoginAsAsync(TestApiFactory factory, string email, string password)
    {
        var client = factory.CreateHttpsClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();
        return client;
    }

    [Fact]
    public async Task GetAuthMe_includes_the_Admin_role_for_the_bootstrap_admin()
    {
        using var factory = CreateFactory();
        using var admin = await LoginAsAsync(factory, TestApiFactory.AdminEmail, TestApiFactory.AdminPassword);

        var me = await admin.GetFromJsonAsync<JsonElement>("/api/auth/me");

        Assert.Equal("Admin", me.GetProperty("role").GetString());
    }

    [Fact]
    public async Task GetAuthMe_includes_the_Member_role_for_a_regular_account()
    {
        using var factory = CreateFactory();
        using var admin = await LoginAsAsync(factory, TestApiFactory.AdminEmail, TestApiFactory.AdminPassword);
        await admin.PostAsJsonAsync("/api/admin/persons", new { email = "role-check@example.com", password = "Member#12345", role = "Member" });

        using var member = await LoginAsAsync(factory, "role-check@example.com", "Member#12345");
        var me = await member.GetFromJsonAsync<JsonElement>("/api/auth/me");

        Assert.Equal("Member", me.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Admin_sees_one_row_per_person_and_provider_including_a_person_with_no_connection()
    {
        using var factory = CreateFactory();
        using var admin = await LoginAsAsync(factory, TestApiFactory.AdminEmail, TestApiFactory.AdminPassword);
        var adminId = (await admin.GetFromJsonAsync<JsonElement>("/api/auth/me")).GetProperty("id").GetGuid();

        var createMember = await admin.PostAsJsonAsync("/api/admin/persons", new { email = "no-connection@example.com", password = "Member#12345", role = "Member" });
        createMember.EnsureSuccessStatusCode();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var tokenEncryption = scope.ServiceProvider.GetRequiredService<ITokenEncryption>();
            var now = DateTimeOffset.UtcNow;
            var connection = new CalendarConnection(Guid.NewGuid(), adminId, CalendarProviders.Google);
            connection.MarkConnected(tokenEncryption.Encrypt("a"), tokenEncryption.Encrypt("r"), now.AddHours(1), now);
            connection.RecordSyncSuccess(now);
            dbContext.CalendarConnections.Add(connection);
            await dbContext.SaveChangesAsync();
        }

        var rows = (await admin.GetFromJsonAsync<JsonElement>("/api/admin/calendar-connections")).EnumerateArray().ToList();

        // 2 people (admin + new member) x 2 providers (Google, Outlook) = 4 rows.
        Assert.Equal(4, rows.Count);
        var adminGoogleRow = rows.Single(r => r.GetProperty("personId").GetGuid() == adminId && r.GetProperty("provider").GetString() == "Google");
        Assert.True(adminGoogleRow.GetProperty("connected").GetBoolean());

        var newMemberRows = rows.Where(r => r.GetProperty("personEmail").GetString() == "no-connection@example.com").ToList();
        Assert.Equal(2, newMemberRows.Count);
        Assert.All(newMemberRows, r => Assert.False(r.GetProperty("connected").GetBoolean()));
        Assert.All(newMemberRows, r => Assert.False(r.GetProperty("hasError").GetBoolean()));
    }

    [Fact]
    public async Task Admin_sees_the_explicit_error_state_for_a_repeatedly_failing_account()
    {
        using var factory = CreateFactory();
        using var admin = await LoginAsAsync(factory, TestApiFactory.AdminEmail, TestApiFactory.AdminPassword);
        var adminId = (await admin.GetFromJsonAsync<JsonElement>("/api/auth/me")).GetProperty("id").GetGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var tokenEncryption = scope.ServiceProvider.GetRequiredService<ITokenEncryption>();
            var now = DateTimeOffset.UtcNow;
            var connection = new CalendarConnection(Guid.NewGuid(), adminId, CalendarProviders.Google);
            connection.MarkConnected(tokenEncryption.Encrypt("a"), tokenEncryption.Encrypt("r"), now.AddHours(1), now.AddDays(-2));
            connection.RecordFailure(now, "token_refresh_failed");
            connection.RecordFailure(now, "token_refresh_failed");
            connection.RecordFailure(now, "token_refresh_failed");
            dbContext.CalendarConnections.Add(connection);
            await dbContext.SaveChangesAsync();
        }

        var rows = (await admin.GetFromJsonAsync<JsonElement>("/api/admin/calendar-connections")).EnumerateArray().ToList();
        var googleRow = rows.Single(r => r.GetProperty("personId").GetGuid() == adminId && r.GetProperty("provider").GetString() == "Google");

        Assert.True(googleRow.GetProperty("hasError").GetBoolean());
        Assert.Equal("token_refresh_failed", googleRow.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Member_cannot_access_the_sync_overview_endpoint()
    {
        using var factory = CreateFactory();
        using var admin = await LoginAsAsync(factory, TestApiFactory.AdminEmail, TestApiFactory.AdminPassword);
        var createMember = await admin.PostAsJsonAsync("/api/admin/persons", new { email = "overview-member@example.com", password = "Member#12345", role = "Member" });
        createMember.EnsureSuccessStatusCode();

        using var member = await LoginAsAsync(factory, "overview-member@example.com", "Member#12345");

        var response = await member.GetAsync("/api/admin/calendar-connections");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
