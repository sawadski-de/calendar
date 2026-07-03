using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Infrastructure;
using Xunit;

namespace IntegrationTests;

[Collection("Postgres")]
public class PersonEndpointsTests(PostgresContainerFixture postgres)
{
    [Fact]
    public async Task Returns_the_roster_with_id_and_email_only_for_an_authenticated_non_admin()
    {
        var factory = new TestApiFactory(postgres.Container.GetConnectionString());
        using var f = factory;
        var adminClient = factory.CreateHttpsClient();

        var login = await adminClient.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestApiFactory.AdminEmail,
            password = TestApiFactory.AdminPassword,
        });
        login.EnsureSuccessStatusCode();

        var memberEmail = $"member-{Guid.NewGuid():N}@test.local";
        var createPerson = await adminClient.PostAsJsonAsync("/api/admin/persons", new
        {
            email = memberEmail,
            password = "Member#12345",
            role = "Member",
        });
        createPerson.EnsureSuccessStatusCode();

        using var memberClient = factory.CreateHttpsClient();
        var memberLogin = await memberClient.PostAsJsonAsync("/api/auth/login", new { email = memberEmail, password = "Member#12345" });
        memberLogin.EnsureSuccessStatusCode();

        var response = await memberClient.GetFromJsonAsync<JsonElement>("/api/persons");
        var entries = response.EnumerateArray().ToArray();

        Assert.Contains(entries, e => e.GetProperty("email").GetString() == memberEmail);
        // Response shape must be exactly { id, email } — no password hash or other Identity field leaks.
        foreach (var entry in entries)
        {
            var propertyNames = entry.EnumerateObject().Select(p => p.Name).ToArray();
            Assert.Equal(new[] { "id", "email" }, propertyNames);
        }
    }
}
