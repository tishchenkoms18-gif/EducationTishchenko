// WebAPI.Tests/UnitTests/Services/BookingServiceTests.cs
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;


namespace WebAPI.Tests.UnitTests.Services;

public class BookingServiceTests
{
    private readonly IBookingService _bookingService;

    public BookingServiceTests()
    {
        var loggerMock = new Mock<ILogger<BookingService>>();
        _bookingService = new BookingService(loggerMock.Object);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldCreateBookingWithPendingStatus()
    {
        // Arrange
        var eventId = Guid.NewGuid();

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
}