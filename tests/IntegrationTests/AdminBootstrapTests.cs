using Domain;
using IntegrationTests.Infrastructure;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IntegrationTests;

[Collection("Postgres")]
public class AdminBootstrapTests(PostgresContainerFixture postgres)
{
    [Fact]
    public async Task Starting_the_api_against_an_empty_database_creates_exactly_one_admin_from_env_vars()
    {
        using var factory = new TestApiFactory(postgres.Container.GetConnectionString());
        using var client = factory.CreateHttpsClient(); // forces host startup: migration + bootstrap

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var admins = await dbContext.People.Where(p => p.Role == PersonRole.Admin).ToListAsync();

        Assert.Single(admins);
        Assert.Equal(TestApiFactory.AdminEmail, admins[0].Email);
    }

    [Fact]
    public async Task Bootstrap_is_idempotent_and_never_creates_a_second_admin()
    {
        using var factory = new TestApiFactory(postgres.Container.GetConnectionString());
        using var client = factory.CreateHttpsClient(); // first bootstrap happens here

        using var scope = factory.Services.CreateScope();

        // Re-invoke the same routine a "next startup" would run, against the now-non-empty database.
        await Api.Startup.AdminBootstrap.EnsureInitialAdminAsync(
            scope.ServiceProvider,
            scope.ServiceProvider.GetRequiredService<IConfiguration>(),
            scope.ServiceProvider.GetRequiredService<ILogger<AdminBootstrapTests>>());

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var admins = await dbContext.People.Where(p => p.Role == PersonRole.Admin).ToListAsync();

        Assert.Single(admins);
    }
}
