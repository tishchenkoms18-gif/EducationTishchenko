// WebAPI.IntegrationTests/BookingRepositoryTests.cs
using WebAPI.DataAccess.Repositories;
using WebAPI.IntegrationTests.Fixtures;
using Xunit;

namespace WebAPI.IntegrationTests;

public class BookingRepositoryTests : BaseRepositoryTest
{
    private readonly BookingRepository _bookingRepository;
    private readonly EventRepository _eventRepository;

    public BookingRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
        _bookingRepository = new BookingRepository(DbContext);
        _eventRepository = new EventRepository(DbContext);
    }

    // Хелпер для создания события (чтобы было к чему привязать бронь)
    private async Task<Event> CreateEventAsync()
    {
        var eventEntity = Event.Create(
            "Test Event", "Desc",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            100);

        await _eventRepository.AddAsync(eventEntity);
        await _eventRepository.SaveChangesAsync();
        return eventEntity;
    }

    [Fact]
    public async Task AddAsync_ShouldSaveBooking()
    {
        // Arrange
        var eventEntity = await CreateEventAsync();
        var booking = Booking.Create(eventEntity.Id);

        // Act
        await _bookingRepository.AddAsync(booking);
        await _bookingRepository.SaveChangesAsync();

        // Assert
        var saved = await DbContext.Bookings.FindAsync(booking.Id);
        Assert.NotNull(saved);
        Assert.Equal(eventEntity.Id, saved.EventId);
        Assert.Equal(BookingStatus.Pending, saved.Status);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnBooking_WhenExists()
    {
        // Arrange
        var eventEntity = await CreateEventAsync();
        var booking = Booking.Create(eventEntity.Id);
        await _bookingRepository.AddAsync(booking);
        await _bookingRepository.SaveChangesAsync();

        // Act
        var result = await _bookingRepository.GetByIdAsync(booking.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(booking.Id, result.Id);
    }

    [Fact]
    public async Task UpdateAsync_ShouldChangeStatus()
    {
        // Arrange
        var eventEntity = await CreateEventAsync();
        var booking = Booking.Create(eventEntity.Id);
        await _bookingRepository.AddAsync(booking);
        await _bookingRepository.SaveChangesAsync();

        // Act
        booking.Confirm();
        await _bookingRepository.UpdateAsync(booking);
        await _bookingRepository.SaveChangesAsync();

        // Assert
        var updated = await _bookingRepository.GetByIdAsync(booking.Id);
        Assert.NotNull(updated);
        Assert.Equal(BookingStatus.Confirmed, updated.Status);
        Assert.NotNull(updated.ProcessedAt);
    }

    [Fact]
    public async Task GetByEventIdAsync_ShouldReturnBookingsForEvent()
    {
        // Arrange
        var event1 = await CreateEventAsync();
        var event2 = await CreateEventAsync();

        await _bookingRepository.AddAsync(Booking.Create(event1.Id));
        await _bookingRepository.AddAsync(Booking.Create(event1.Id));
        await _bookingRepository.AddAsync(Booking.Create(event2.Id));
        await _bookingRepository.SaveChangesAsync();

        // Act
        var result = await _bookingRepository.GetByEventIdAsync(event1.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, b => Assert.Equal(event1.Id, b.EventId));
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnFilteredBookings()
    {
        // Arrange
        var eventEntity = await CreateEventAsync();
        
        var pending = Booking.Create(eventEntity.Id);
        var confirmed = Booking.Create(eventEntity.Id);
        confirmed.Confirm();

        await _bookingRepository.AddAsync(pending);
        await _bookingRepository.AddAsync(confirmed);
        await _bookingRepository.SaveChangesAsync();

        // Act
        var pendingResult = await _bookingRepository.GetByStatusAsync(BookingStatus.Pending);
        var confirmedResult = await _bookingRepository.GetByStatusAsync(BookingStatus.Confirmed);

        // Assert
        Assert.Single(pendingResult);
        Assert.Single(confirmedResult);
        Assert.Equal(pending.Id, pendingResult[0].Id);
        Assert.Equal(confirmed.Id, confirmedResult[0].Id);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllBookings()
    {
        // Arrange
        var eventEntity = await CreateEventAsync();
        await _bookingRepository.AddAsync(Booking.Create(eventEntity.Id));
        await _bookingRepository.AddAsync(Booking.Create(eventEntity.Id));
        await _bookingRepository.SaveChangesAsync();

        // Act
        var result = await _bookingRepository.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }
}