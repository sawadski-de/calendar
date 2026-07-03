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
}
