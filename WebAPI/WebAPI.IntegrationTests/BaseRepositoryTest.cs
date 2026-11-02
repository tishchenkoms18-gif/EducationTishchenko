// WebAPI.IntegrationTests/BaseRepositoryTest.cs
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WebAPI.DataAccess;
using WebAPI.IntegrationTests.Fixtures;
using Xunit;

namespace WebAPI.IntegrationTests;

[Collection("PostgresCollection")]
public abstract class BaseRepositoryTest : IAsyncLifetime
{
    protected readonly AppDbContext DbContext;

    protected BaseRepositoryTest(PostgresFixture fixture)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        DbContext = new AppDbContext(options);
    }

    public async Task InitializeAsync()
    {
        // Сбрасываем пул соединений Npgsql, 
        // иначе EnsureDeleted может упасть из-за активных соединений
        NpgsqlConnection.ClearAllPools();

        // Удаляем БД и создаём её заново через миграции
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
    }
}