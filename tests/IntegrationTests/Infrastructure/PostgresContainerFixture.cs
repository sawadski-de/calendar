using Testcontainers.PostgreSql;
using Xunit;

namespace IntegrationTests.Infrastructure;

/// <summary>
/// One real Postgres container for the whole test run (project-context: "no DB mocking for
/// security-relevant rules"). Each <see cref="TestApiFactory"/> creates its own database inside
/// this container for isolation, so tests never share mutable state.
/// </summary>
public class PostgresContainerFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("postgres")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public Task InitializeAsync() => Container.StartAsync();

    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}

[CollectionDefinition("Postgres")]
public class PostgresCollection : ICollectionFixture<PostgresContainerFixture>;
