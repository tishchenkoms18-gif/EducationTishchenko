// WebAPI.Tests/UnitTests/Services/BookingServiceTests.cs
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;


namespace WebAPI.Tests.UnitTests.Services;

public class BookingServiceTests
{
    private readonly Mock<IEventService> _eventServiceMock;
    private readonly BookingService _bookingService;

    public BookingServiceTests()
    {
        _eventServiceMock = new Mock<IEventService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        _bookingService = new BookingService(loggerMock.Object, _eventServiceMock.Object);
    }

    // =============================================
    // УСПЕШНЫЕ СЦЕНАРИИ
    // =============================================

    [Fact]
    public async Task CreateBookingAsync_ShouldCreateBookingWithPendingStatus()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingEvent = CreateTestEvent(eventId, totalSeats: 10);

        _eventServiceMock
            .Setup(x => x.GetByIdEventAsync(eventId))    // ✅ async
            .ReturnsAsync(existingEvent);

        // Act
        var booking = await _bookingService.CreateBookingAsync(eventId);

        // Assert
        Assert.NotEqual(Guid.Empty, booking.Id);
        Assert.Equal(eventId, booking.EventId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Null(booking.ProcessedAt);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldDecreaseAvailableSeats()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingEvent = CreateTestEvent(eventId, totalSeats: 5);

        _eventServiceMock
            .Setup(x => x.GetByIdEventAsync(eventId))
            .ReturnsAsync(existingEvent);

        // Act
        await _bookingService.CreateBookingAsync(eventId);

        // Assert
        Assert.Equal(4, existingEvent.AvailableSeats);
    }

    [Fact]
    public async Task CreateMultipleBookings_ShouldHaveUniqueIds()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingEvent = CreateTestEvent(eventId, totalSeats: 10);

        _eventServiceMock
            .Setup(x => x.GetByIdEventAsync(eventId))
            .ReturnsAsync(existingEvent);

        // Act
        var booking1 = await _bookingService.CreateBookingAsync(eventId);
        var booking2 = await _bookingService.CreateBookingAsync(eventId);
        var booking3 = await _bookingService.CreateBookingAsync(eventId);

        // Assert
        Assert.NotEqual(booking1.Id, booking2.Id);
        Assert.NotEqual(booking2.Id, booking3.Id);
        Assert.NotEqual(booking1.Id, booking3.Id);
        Assert.Equal(7, existingEvent.AvailableSeats); // 10 - 3
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldReturnBooking_WhenExists()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingEvent = CreateTestEvent(eventId, totalSeats: 10);

        _eventServiceMock
            .Setup(x => x.GetByIdEventAsync(eventId))
            .ReturnsAsync(existingEvent);

        var created = await _bookingService.CreateBookingAsync(eventId);

        // Act
        var booking = await _bookingService.GetBookingByIdAsync(created.Id);

        // Assert
        Assert.Equal(created.Id, booking.Id);
        Assert.Equal(created.EventId, booking.EventId);
        Assert.Equal(created.Status, booking.Status);
    }

    [Fact]
    public async Task ConfirmBookingAsync_ShouldChangeStatusToConfirmed()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingEvent = CreateTestEvent(eventId, totalSeats: 10);

        _eventServiceMock
            .Setup(x => x.GetByIdEventAsync(eventId))
            .ReturnsAsync(existingEvent);

        var booking = await _bookingService.CreateBookingAsync(eventId);

        // Act
        await _bookingService.ConfirmBookingAsync(booking.Id);
        var updated = await _bookingService.GetBookingByIdAsync(booking.Id);

        // Assert
        Assert.Equal(BookingStatus.Confirmed, updated.Status);
        Assert.NotNull(updated.ProcessedAt);
    }

    [Fact]
    public async Task RejectBookingAsync_ShouldChangeStatusToRejected()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingEvent = CreateTestEvent(eventId, totalSeats: 10);

        _eventServiceMock
            .Setup(x => x.GetByIdEventAsync(eventId))
            .ReturnsAsync(existingEvent);

        var booking = await _bookingService.CreateBookingAsync(eventId);

        // Act
        await _bookingService.RejectBookingAsync(booking.Id);
        var updated = await _bookingService.GetBookingByIdAsync(booking.Id);

        // Assert
        Assert.Equal(BookingStatus.Rejected, updated.Status);
        Assert.NotNull(updated.ProcessedAt);
    }

    [Fact]
    public async Task RejectBookingAsync_ShouldReleaseSeats()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingEvent = CreateTestEvent(eventId, totalSeats: 1);

        _eventServiceMock
            .Setup(x => x.GetByIdEventAsync(eventId))
            .ReturnsAsync(existingEvent);

        var booking = await _bookingService.CreateBookingAsync(eventId);
        Assert.Equal(0, existingEvent.AvailableSeats);

        // Act
        await _bookingService.RejectBookingAsync(booking.Id);

        // Assert
        Assert.Equal(1, existingEvent.AvailableSeats);
    }

    [Fact]
    public async Task RejectBookingAsync_ThenNewBooking_ShouldSucceed()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingEvent = CreateTestEvent(eventId, totalSeats: 1);

        _eventServiceMock
            .Setup(x => x.GetByIdEventAsync(eventId))
            .ReturnsAsync(existingEvent);

        var firstBooking = await _bookingService.CreateBookingAsync(eventId);

        // Act
        await _bookingService.RejectBookingAsync(firstBooking.Id);
        var secondBooking = await _bookingService.CreateBookingAsync(eventId);

        // Assert
        Assert.NotNull(secondBooking);
        Assert.NotEqual(firstBooking.Id, secondBooking.Id);
        Assert.Equal(0, existingEvent.AvailableSeats);
    }

    // =============================================
    // НЕУСПЕШНЫЕ СЦЕНАРИИ
    // =============================================

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowArgumentException_WhenEventIdIsEmpty()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _bookingService.CreateBookingAsync(Guid.Empty));
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowEventNotFoundException_WhenEventDoesNotExist()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        _eventServiceMock
            .Setup(x => x.GetByIdEventAsync(eventId))
            .ThrowsAsync(new EventNotFoundException(eventId));   // ✅ async

        // Act & Assert
        await Assert.ThrowsAsync<EventNotFoundException>(() =>
            _bookingService.CreateBookingAsync(eventId));
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowNoAvailableSeatsException_WhenNoSeats()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingEvent = CreateTestEvent(eventId, totalSeats: 1);

        _eventServiceMock
            .Setup(x => x.GetByIdEventAsync(eventId))
            .ReturnsAsync(existingEvent);

        // Занимаем единственное место
        await _bookingService.CreateBookingAsync(eventId);

        // Act & Assert
        await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            _bookingService.CreateBookingAsync(eventId));
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldThrowBookingNotFoundException_WhenNotExists()
    {
        var nonExistentId = Guid.NewGuid();

        await Assert.ThrowsAsync<BookingNotFoundException>(() =>
            _bookingService.GetBookingByIdAsync(nonExistentId));
    }

    [Fact]
    public async Task ConfirmBookingAsync_ShouldThrowBookingNotFoundException_WhenNotExists()
    {
        var nonExistentId = Guid.NewGuid();

        await Assert.ThrowsAsync<BookingNotFoundException>(() =>
            _bookingService.ConfirmBookingAsync(nonExistentId));
    }

    [Fact]
    public async Task RejectBookingAsync_ShouldThrowBookingNotFoundException_WhenNotExists()
    {
        var nonExistentId = Guid.NewGuid();

        await Assert.ThrowsAsync<BookingNotFoundException>(() =>
            _bookingService.RejectBookingAsync(nonExistentId));
    }

    // =============================================
    // ТЕСТЫ НА КОНКУРЕНТНОСТЬ
    // =============================================

    [Fact]
    public async Task ConcurrentBookings_ShouldNotExceedTotalSeats()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingEvent = CreateTestEvent(eventId, totalSeats: 5);

        _eventServiceMock
            .Setup(x => x.GetByIdEventAsync(eventId))
            .ReturnsAsync(existingEvent);

        const int concurrentRequests = 20;
        int successCount = 0;
        int conflictCount = 0;
        var lockObj = new object();

        // Act
        var tasks = Enumerable.Range(0, concurrentRequests).Select(_ => Task.Run(async () =>
        {
            try
            {
                await _bookingService.CreateBookingAsync(eventId);
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
        Assert.Equal(0, existingEvent.AvailableSeats);
    }

    [Fact]
    public async Task ConcurrentBookings_ShouldHaveUniqueIds()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingEvent = CreateTestEvent(eventId, totalSeats: 10);

        _eventServiceMock
            .Setup(x => x.GetByIdEventAsync(eventId))
            .ReturnsAsync(existingEvent);

        var ids = new ConcurrentBag<Guid>();

        // Act
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
        {
            var booking = await _bookingService.CreateBookingAsync(eventId);
            ids.Add(booking.Id);
        }));

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, ids.Count);
        Assert.Equal(10, ids.Distinct().Count());
    }

    // =============================================
    // ХЕЛПЕР
    // =============================================

    private static Event CreateTestEvent(Guid id, int totalSeats = 10)
    {
        var eventEntity = Event.Create(
            "Test Event",
            null,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            totalSeats);

        // Устанавливаем нужный Id через рефлексию
        typeof(Event)
            .GetProperty(nameof(Event.Id))!
            .SetValue(eventEntity, id);

        return eventEntity;
    }
}