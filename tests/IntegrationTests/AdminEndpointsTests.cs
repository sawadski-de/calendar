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
public class AdminEndpointsTests(PostgresContainerFixture postgres)
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
    public async Task Admin_can_create_a_member_account()
    {
        using var factory = CreateFactory();
        using var admin = await LoginAsAsync(factory, TestApiFactory.AdminEmail, TestApiFactory.AdminPassword);

        var response = await admin.PostAsJsonAsync("/api/admin/persons", new
        {
            email = "mara@example.com",
            password = "Member#12345",
            role = "Member",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Demoting_the_sole_admin_is_rejected_with_409_and_stable_code()
    {
        using var factory = CreateFactory();
        using var admin = await LoginAsAsync(factory, TestApiFactory.AdminEmail, TestApiFactory.AdminPassword);

        var me = await admin.GetFromJsonAsync<JsonElement>("/api/auth/me");
        var adminId = me.GetProperty("id").GetString();

        var response = await admin.PutAsJsonAsync($"/api/admin/persons/{adminId}/role", new { role = "Member" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("last-admin-cannot-be-demoted", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Demoting_an_admin_succeeds_once_a_second_admin_exists()
    {
        using var factory = CreateFactory();
        using var admin = await LoginAsAsync(factory, TestApiFactory.AdminEmail, TestApiFactory.AdminPassword);

        var me = await admin.GetFromJsonAsync<JsonElement>("/api/auth/me");
        var adminId = me.GetProperty("id").GetString();

        var createSecondAdmin = await admin.PostAsJsonAsync("/api/admin/persons", new
        {
            email = "admin2@example.com",
            password = "Admin2#12345",
            role = "Admin",
        });
        createSecondAdmin.EnsureSuccessStatusCode();

        var response = await admin.PutAsJsonAsync($"/api/admin/persons/{adminId}/role", new { role = "Member" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Member_cannot_access_admin_endpoints_even_with_a_valid_session()
    {
        using var factory = CreateFactory();
        using var admin = await LoginAsAsync(factory, TestApiFactory.AdminEmail, TestApiFactory.AdminPassword);

        var createMember = await admin.PostAsJsonAsync("/api/admin/persons", new
        {
            email = "member-only@example.com",
            password = "Member#12345",
            role = "Member",
        });
        createMember.EnsureSuccessStatusCode();

        using var member = await LoginAsAsync(factory, "member-only@example.com", "Member#12345");

        var response = await member.PostAsJsonAsync("/api/admin/persons", new
        {
            email = "should-not-be-created@example.com",
            password = "Whatever#123",
            role = "Member",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Concurrent_demote_requests_against_a_two_admin_system_never_leave_zero_admins()
    {
        using var factory = CreateFactory();
        using var admin = await LoginAsAsync(factory, TestApiFactory.AdminEmail, TestApiFactory.AdminPassword);

        var me = await admin.GetFromJsonAsync<JsonElement>("/api/auth/me");
        var admin1Id = me.GetProperty("id").GetString();

        var createSecondAdmin = await admin.PostAsJsonAsync("/api/admin/persons", new
        {
            email = "concurrent-admin2@example.com",
            password = "Admin2#12345",
            role = "Admin",
        });
        createSecondAdmin.EnsureSuccessStatusCode();
        var admin2Id = (await createSecondAdmin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        // Fire both demotions at once, on the same client, so they race for the same "how many
        // admins are there right now?" read inside RoleChangeService.ChangeRoleAsync.
        var demoteAdmin1 = admin.PutAsJsonAsync($"/api/admin/persons/{admin1Id}/role", new { role = "Member" });
        var demoteAdmin2 = admin.PutAsJsonAsync($"/api/admin/persons/{admin2Id}/role", new { role = "Member" });
        var responses = await Task.WhenAll(demoteAdmin1, demoteAdmin2);

        var succeededCount = responses.Count(r => r.StatusCode == HttpStatusCode.NoContent);
        var rejectedCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(1, succeededCount);
        Assert.Equal(1, rejectedCount);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var remainingAdmins = await dbContext.People.CountAsync(p => p.Role == PersonRole.Admin);
        Assert.Equal(1, remainingAdmins);
    }

    [Fact]
    public async Task Demoted_admins_existing_session_loses_admin_access_immediately_without_relogin()
    {
        using var factory = CreateFactory();
        using var admin = await LoginAsAsync(factory, TestApiFactory.AdminEmail, TestApiFactory.AdminPassword);

        var me = await admin.GetFromJsonAsync<JsonElement>("/api/auth/me");
        var adminId = me.GetProperty("id").GetString();

        var createSecondAdmin = await admin.PostAsJsonAsync("/api/admin/persons", new
        {
            email = "admin2@example.com",
            password = "Admin2#12345",
            role = "Admin",
        });
        createSecondAdmin.EnsureSuccessStatusCode();

        var demote = await admin.PutAsJsonAsync($"/api/admin/persons/{adminId}/role", new { role = "Member" });
        Assert.Equal(HttpStatusCode.NoContent, demote.StatusCode);

        // Same HttpClient/cookie as before demotion — no re-login. Proves Person.Role is checked
        // fresh per request rather than trusting a role claim baked into the cookie at sign-in.
        var response = await admin.PostAsJsonAsync("/api/admin/persons", new
        {
            email = "should-not-be-created-either@example.com",
            password = "Whatever#123",
            role = "Member",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
