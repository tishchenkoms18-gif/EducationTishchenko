// WebAPI.Tests/UnitTests/Services/BookingServiceTests.cs
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebAPI.DataAccess;
using Xunit;

namespace WebAPI.Tests.UnitTests.Services;

public class BookingServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public BookingServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose() => _serviceProvider.Dispose();

    // =============================================
    // ХЕЛПЕР
    // =============================================

    private static IEventService GetEventService(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<IEventService>();

    private static IBookingService GetBookingService(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<IBookingService>();

    private static async Task<Event> CreateTestEventAsync(
        IEventService eventService, int totalSeats = 10)
    {
        return await eventService.CreateEventAsync(
            "Test Event", null,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            totalSeats);
    }

    // =============================================
    // УСПЕШНЫЕ СЦЕНАРИИ
    // =============================================

    [Fact]
    public async Task CreateBookingAsync_ShouldCreateBookingWithPendingStatus()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = GetEventService(scope);
        var bookingService = GetBookingService(scope);

        var existingEvent = await CreateTestEventAsync(eventService, totalSeats: 10);

        // Act
        var booking = await bookingService.CreateBookingAsync(existingEvent.Id);

        // Assert
        Assert.NotEqual(Guid.Empty, booking.Id);
        Assert.Equal(existingEvent.Id, booking.EventId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Null(booking.ProcessedAt);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldDecreaseAvailableSeats()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = GetEventService(scope);
        var bookingService = GetBookingService(scope);

        var existingEvent = await CreateTestEventAsync(eventService, totalSeats: 5);

        // Act
        await bookingService.CreateBookingAsync(existingEvent.Id);

        // Assert — загружаем событие заново, чтобы увидеть изменение
        var updated = await eventService.GetByIdEventAsync(existingEvent.Id);
        Assert.Equal(4, updated.AvailableSeats);
    }

    [Fact]
    public async Task CreateMultipleBookings_ShouldHaveUniqueIds()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = GetEventService(scope);
        var bookingService = GetBookingService(scope);

        var existingEvent = await CreateTestEventAsync(eventService, totalSeats: 10);

        // Act
        var b1 = await bookingService.CreateBookingAsync(existingEvent.Id);
        var b2 = await bookingService.CreateBookingAsync(existingEvent.Id);
        var b3 = await bookingService.CreateBookingAsync(existingEvent.Id);

        // Assert
        Assert.NotEqual(b1.Id, b2.Id);
        Assert.NotEqual(b2.Id, b3.Id);
        Assert.NotEqual(b1.Id, b3.Id);

        var updated = await eventService.GetByIdEventAsync(existingEvent.Id);
        Assert.Equal(7, updated.AvailableSeats);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldReturnBooking_WhenExists()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = GetEventService(scope);
        var bookingService = GetBookingService(scope);

        var existingEvent = await CreateTestEventAsync(eventService, totalSeats: 10);
        var created = await bookingService.CreateBookingAsync(existingEvent.Id);

        // Act
        var booking = await bookingService.GetBookingByIdAsync(created.Id);

        // Assert
        Assert.Equal(created.Id, booking.Id);
        Assert.Equal(created.EventId, booking.EventId);
        Assert.Equal(created.Status, booking.Status);
    }

    [Fact]
    public async Task ConfirmBookingAsync_ShouldChangeStatusToConfirmed()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = GetEventService(scope);
        var bookingService = GetBookingService(scope);

        var existingEvent = await CreateTestEventAsync(eventService, totalSeats: 10);
        var booking = await bookingService.CreateBookingAsync(existingEvent.Id);

        // Act
        await bookingService.ConfirmBookingAsync(booking.Id);
        var updated = await bookingService.GetBookingByIdAsync(booking.Id);

        // Assert
        Assert.Equal(BookingStatus.Confirmed, updated.Status);
        Assert.NotNull(updated.ProcessedAt);
    }

    [Fact]
    public async Task RejectBookingAsync_ShouldChangeStatusToRejected()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = GetEventService(scope);
        var bookingService = GetBookingService(scope);

        var existingEvent = await CreateTestEventAsync(eventService, totalSeats: 10);
        var booking = await bookingService.CreateBookingAsync(existingEvent.Id);

        // Act
        await bookingService.RejectBookingAsync(booking.Id);
        var updated = await bookingService.GetBookingByIdAsync(booking.Id);

        // Assert
        Assert.Equal(BookingStatus.Rejected, updated.Status);
        Assert.NotNull(updated.ProcessedAt);
    }

    [Fact]
    public async Task RejectBookingAsync_ShouldReleaseSeats()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = GetEventService(scope);
        var bookingService = GetBookingService(scope);

        var existingEvent = await CreateTestEventAsync(eventService, totalSeats: 1);
        var booking = await bookingService.CreateBookingAsync(existingEvent.Id);

        // Проверяем, что места не осталось
        var afterBooking = await eventService.GetByIdEventAsync(existingEvent.Id);
        Assert.Equal(0, afterBooking.AvailableSeats);

        // Act
        await bookingService.RejectBookingAsync(booking.Id);

        // Assert
        var afterReject = await eventService.GetByIdEventAsync(existingEvent.Id);
        Assert.Equal(1, afterReject.AvailableSeats);
    }

    // =============================================
    // НЕУСПЕШНЫЕ СЦЕНАРИИ
    // =============================================

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowArgumentException_WhenEventIdIsEmpty()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = GetBookingService(scope);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            bookingService.CreateBookingAsync(Guid.Empty));
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowEventNotFoundException_WhenEventDoesNotExist()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = GetBookingService(scope);

        var nonExistentId = Guid.NewGuid();

        await Assert.ThrowsAsync<EventNotFoundException>(() =>
            bookingService.CreateBookingAsync(nonExistentId));
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowNoAvailableSeatsException_WhenNoSeats()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = GetEventService(scope);
        var bookingService = GetBookingService(scope);

        var existingEvent = await CreateTestEventAsync(eventService, totalSeats: 1);
        await bookingService.CreateBookingAsync(existingEvent.Id);

        // Act & Assert
        await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            bookingService.CreateBookingAsync(existingEvent.Id));
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldThrowBookingNotFoundException_WhenNotExists()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = GetBookingService(scope);

        var nonExistentId = Guid.NewGuid();

        await Assert.ThrowsAsync<BookingNotFoundException>(() =>
            bookingService.GetBookingByIdAsync(nonExistentId));
    }

    // =============================================
    // ТЕСТЫ НА КОНКУРЕНТНОСТЬ
    // =============================================

    [Fact]
    public async Task ConcurrentBookings_ShouldNotExceedTotalSeats()
    {
        // Arrange — создаём событие на 5 мест
        Guid eventId;

        using (var scope = _serviceProvider.CreateScope())
        {
            var eventService = GetEventService(scope);
            var existingEvent = await CreateTestEventAsync(eventService, totalSeats: 5);
            eventId = existingEvent.Id;
        }

        const int concurrentRequests = 20;
        int successCount = 0;
        int conflictCount = 0;
        var lockObj = new object();

        // Act — каждый запрос в СВОЁМ scope
        var tasks = Enumerable.Range(0, concurrentRequests).Select(_ => Task.Run(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var bookingService = GetBookingService(scope);

            try
            {
                await bookingService.CreateBookingAsync(eventId);
                lock (lockObj) successCount++;
            }
            catch (NoAvailableSeatsException)
            {
                lock (lockObj) conflictCount++;
            }
        }));

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(5, successCount);
        Assert.Equal(15, conflictCount);

        // Проверяем, что мест не осталось
        using (var scope = _serviceProvider.CreateScope())
        {
            var eventService = GetEventService(scope);
            var updated = await eventService.GetByIdEventAsync(eventId);
            Assert.Equal(0, updated.AvailableSeats);
        }
    }

    [Fact]
    public async Task ConcurrentBookings_ShouldHaveUniqueIds()
    {
        Guid eventId;

        using (var scope = _serviceProvider.CreateScope())
        {
            var eventService = GetEventService(scope);
            var existingEvent = await CreateTestEventAsync(eventService, totalSeats: 10);
            eventId = existingEvent.Id;
        }

        var ids = new ConcurrentBag<Guid>();

        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var bookingService = GetBookingService(scope);
            var booking = await bookingService.CreateBookingAsync(eventId);
            ids.Add(booking.Id);
        }));

        await Task.WhenAll(tasks);

        Assert.Equal(10, ids.Count);
        Assert.Equal(10, ids.Distinct().Count());
    }
}