// WebAPI.Tests/UnitTests/Services/BookingServiceTests.cs
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;


namespace WebAPI.Tests.UnitTests.Services;

public class BookingServiceTests
{
    private readonly Mock<IEventService> _eventServiceMock;
    private readonly IBookingService _bookingService;
    

    public BookingServiceTests()
    {

        _eventServiceMock = new Mock<IEventService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        _bookingService = new BookingService(loggerMock.Object, _eventServiceMock.Object);

       
        
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldCreateBookingWithPendingStatus()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingEvent = CreateEventWithId(eventId);


_eventServiceMock
        .Setup(x => x.GetByIdEvent(eventId))
        .Returns(existingEvent); 

        // Act
        var booking = await _bookingService.CreateBookingAsync(eventId);

        // Assert
        Assert.NotEqual(Guid.Empty, booking.Id);
        Assert.Equal(eventId, booking.EventId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Null(booking.ProcessedAt);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldReturnBooking_WhenExists()
    {
        // Arrange
        var eventId = Guid.NewGuid();
         var existingEvent = CreateEventWithId(eventId);
        _eventServiceMock.Setup(x => x.GetByIdEvent(eventId)).Returns(existingEvent);
        var created = await _bookingService.CreateBookingAsync(eventId);

        // Act
        var booking = await _bookingService.GetBookingByIdAsync(created.Id);

        // Assert
        Assert.Equal(created.Id, booking.Id);
        Assert.Equal(created.EventId, booking.EventId);
        Assert.Equal(created.Status, booking.Status);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldThrowBookingNotFoundException_WhenNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<BookingNotFoundException>(() =>
            _bookingService.GetBookingByIdAsync(nonExistentId));
    }

    [Fact]
    public async Task ConfirmBookingAsync_ShouldChangeStatusToConfirmed()
    {
        // Arrange
        var eventId = Guid.NewGuid();
         var existingEvent = CreateEventWithId(eventId);
        _eventServiceMock.Setup(x => x.GetByIdEvent(eventId)).Returns(existingEvent);
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
         var existingEvent = CreateEventWithId(eventId);
        _eventServiceMock.Setup(x => x.GetByIdEvent(eventId)).Returns(existingEvent);
        var booking = await _bookingService.CreateBookingAsync(eventId);

        // Act
        await _bookingService.RejectBookingAsync(booking.Id);
        var updated = await _bookingService.GetBookingByIdAsync(booking.Id);

        // Assert
        Assert.Equal(BookingStatus.Rejected, updated.Status);
        Assert.NotNull(updated.ProcessedAt);
    }

    // Неуспешные сценарии

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowArgumentException_WhenEventIdIsEmpty()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _bookingService.CreateBookingAsync(Guid.Empty));
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldThrowBookingNotFoundException_WhenBookingDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<BookingNotFoundException>(() => _bookingService.GetBookingByIdAsync(nonExistentId));
    }

    [Fact]
    public async Task ConfirmBookingAsync_ShouldThrowBookingNotFoundException_WhenBookingDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<BookingNotFoundException>(() => _bookingService.ConfirmBookingAsync(nonExistentId));
    }

    [Fact]
    public async Task RejectBookingAsync_ShouldThrowBookingNotFoundException_WhenBookingDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        // Act & Assert
        await Assert.ThrowsAsync<BookingNotFoundException>(() => _bookingService.RejectBookingAsync(nonExistentId));
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowEventNotFoundException_WhenEventDoesNotExist()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _eventServiceMock
            .Setup(x => x.GetByIdEvent(eventId))
            .Returns((Event)null); // событие не найдено

        // Act & Assert
        await Assert.ThrowsAsync<EventNotFoundException>(() =>
            _bookingService.CreateBookingAsync(eventId));
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowEventNotFoundException_WhenEventWasDeleted()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _eventServiceMock
            .Setup(x => x.GetByIdEvent(eventId))
            .Throws(new EventNotFoundException(eventId)); // событие удалено

        // Act & Assert
        await Assert.ThrowsAsync<EventNotFoundException>(() =>
            _bookingService.CreateBookingAsync(eventId));
    }
    private Event CreateEventWithId(Guid id, string title = "Test Event")
    {
        return (Event)Activator.CreateInstance(
            typeof(Event),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new object[] { id, title, null, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), DateTime.UtcNow },
            null)!;
    }
    [Fact]
    public async Task CreateBookingAsync_ShouldCreateBooking_WhenEventExists()
{
    // Arrange
    var eventId = Guid.NewGuid();
    var existingEvent = CreateEventWithId(eventId); // 👈 используем рефлексию

    _eventServiceMock
        .Setup(x => x.GetByIdEvent(eventId))
        .Returns(existingEvent);

    // Act
    var booking = await _bookingService.CreateBookingAsync(eventId);

    // Assert
    Assert.Equal(eventId, booking.EventId);
    Assert.Equal(BookingStatus.Pending, booking.Status);
}
}