using Microsoft.EntityFrameworkCore;
using WebAPI.DataAccess;
using WebAPI.IntegrationTests.Fixtures;
using Xunit;
using Npgsql;

namespace WebAPI.IntegrationTests;

public class MigrationTests : BaseRepositoryTest
{
    public MigrationTests(PostgresFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Database_ShouldHaveAppliedInitialCreateMigration()
    {
        // Assert
        var appliedMigrations = await DbContext.Database.GetAppliedMigrationsAsync();
        var pendingMigrations = await DbContext.Database.GetPendingMigrationsAsync();

        Assert.Contains(appliedMigrations, m => m.EndsWith("InitialCreate"));
        Assert.Empty(pendingMigrations);
    }

    [Fact]
    public async Task Database_ShouldContainEventsAndBookingsTables()
    {
        // Act
        var tables = await DbContext.Database
            .SqlQueryRaw<string>("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'")
            .ToListAsync();

        // Assert
        Assert.Contains("events", tables);
        Assert.Contains("bookings", tables);
    }

    [Fact]
    public async Task Database_ShouldEnforceForeignKeyConstraint()
    {
        // Arrange
        var invalidBooking = Booking.Create(Guid.NewGuid()); // Несуществующий EventId
        DbContext.Bookings.Add(invalidBooking);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => DbContext.SaveChangesAsync());
        Assert.IsType<PostgresException>(exception.InnerException); // Ошибка FK
    }

    [Fact]
    public async Task Database_ShouldCascadeDeleteBookings_WhenEventDeleted()
    {
        // Arrange
        var eventEntity = Event.Create("Test", null, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
        DbContext.Events.Add(eventEntity);
        await DbContext.SaveChangesAsync();

        var booking = Booking.Create(eventEntity.Id);
        DbContext.Bookings.Add(booking);
        await DbContext.SaveChangesAsync();

        // Act
        DbContext.Events.Remove(eventEntity);
        await DbContext.SaveChangesAsync();

        // Assert
        var deletedBooking = await DbContext.Bookings.FindAsync(booking.Id);
        Assert.Null(deletedBooking); // Бронь удалилась каскадом
    }
}