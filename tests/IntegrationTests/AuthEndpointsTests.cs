using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Infrastructure;
using Xunit;

namespace IntegrationTests;

[Collection("Postgres")]
public class AuthEndpointsTests(PostgresContainerFixture postgres)
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
    public async Task Login_with_bootstrapped_admin_credentials_succeeds()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestApiFactory.AdminEmail,
            password = TestApiFactory.AdminPassword,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_sets_an_HttpOnly_Secure_SameSiteLax_cookie()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestApiFactory.AdminEmail,
            password = TestApiFactory.AdminPassword,
        });

        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        var authCookie = Assert.Single(cookies!, c => c.StartsWith(".AspNetCore.Identity.Application=", StringComparison.Ordinal));

        Assert.Contains("httponly", authCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", authCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", authCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401_problem_with_stable_code()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestApiFactory.AdminEmail,
            password = "wrong-password",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid-credentials", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Unauthenticated_request_to_a_protected_endpoint_returns_401()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsync("/api/auth/logout", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_then_me_then_logout_then_me_again_returns_401()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateHttpsClient();

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestApiFactory.AdminEmail,
            password = TestApiFactory.AdminPassword,
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        var logout = await client.PostAsync("/api/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        var meAfterLogout = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meAfterLogout.StatusCode);
    }

    [Fact]
    public async Task Me_returns_the_persons_role_from_the_claim_baked_in_at_login()
    {
        using var factory = CreateFactory();
        using var admin = await LoginAsAsync(factory, TestApiFactory.AdminEmail, TestApiFactory.AdminPassword);

        await admin.PostAsJsonAsync("/api/admin/persons", new
        {
            email = "member-role-claim@example.com",
            password = "Member#12345",
            role = "Member",
        });

        using var member = await LoginAsAsync(factory, "member-role-claim@example.com", "Member#12345");
        var me = await member.GetFromJsonAsync<JsonElement>("/api/auth/me");

        Assert.Equal("Member", me.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Me_keeps_returning_the_pre_change_role_until_the_next_login_after_a_role_change()
    {
        // Documents the accepted tradeoff behind reading the role from a claim instead of the DB
        // (code review follow-up, Epic 2): /api/auth/me is a nav-gating convenience only, so a role
        // change taking effect here only after the next login is fine — actual authorization
        // (AdminOnlyAuthorizationHandler) always re-reads Person.Role fresh, unaffected by this claim.
        using var factory = CreateFactory();
        using var admin = await LoginAsAsync(factory, TestApiFactory.AdminEmail, TestApiFactory.AdminPassword);

        var create = await admin.PostAsJsonAsync("/api/admin/persons", new
        {
            email = "promoted-after-login@example.com",
            password = "Member#12345",
            role = "Member",
        });
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var personId = created.GetProperty("id").GetString();

        using var member = await LoginAsAsync(factory, "promoted-after-login@example.com", "Member#12345");

        var promote = await admin.PutAsJsonAsync($"/api/admin/persons/{personId}/role", new { role = "Admin" });
        Assert.Equal(HttpStatusCode.NoContent, promote.StatusCode);

        // Same cookie as before the promotion — no re-login.
        var meAfterPromotion = await member.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.Equal("Member", meAfterPromotion.GetProperty("role").GetString());

        using var memberAfterRelogin = await LoginAsAsync(factory, "promoted-after-login@example.com", "Member#12345");
        var meAfterRelogin = await memberAfterRelogin.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.Equal("Admin", meAfterRelogin.GetProperty("role").GetString());
    }
}
