// WebAPI.IntegrationTests/Fixtures/PostgresFixture.cs
using Testcontainers.PostgreSql;
using Xunit;

namespace WebAPI.IntegrationTests.Fixtures;

public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;

    public PostgresFixture()
    {
        _dbContainer = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("eventapi_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public string ConnectionString => _dbContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }
}

// WebAPI.IntegrationTests/Fixtures/PostgresCollection.cs
[CollectionDefinition("PostgresCollection")]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
}