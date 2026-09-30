// WebAPI.Tests/UnitTests/Services/EventServiceTests.cs
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;

namespace WebAPI.Tests.UnitTests.Services;

public class EventServiceTests
{
    private readonly EventService _eventService;

    public EventServiceTests()
    {
        var loggerMock = new Mock<ILogger<EventService>>();
        _eventService = new EventService(loggerMock.Object);
    }

    // =============================================
    // УСПЕШНЫЕ СЦЕНАРИИ
    // =============================================

    [Fact]
    public async Task CreateEvent_ShouldCreateAndReturnEvent()
    {
        // Arrange
        var title = "Конференция .NET";
        var description = "Ежегодная конференция";
        var startAt = DateTime.UtcNow.AddDays(5);
        var endAt = DateTime.UtcNow.AddDays(6);
        var totalSeats = 100;

        // Act
        var result = await _eventService.CreateEventAsync(title, description, startAt, endAt, totalSeats);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(title, result.Title);
        Assert.Equal(description, result.Description);
        Assert.Equal(startAt.ToUniversalTime(), result.StartAt);
        Assert.Equal(endAt.ToUniversalTime(), result.EndAt);
        Assert.Equal(totalSeats, result.TotalSeats);
        Assert.Equal(totalSeats, result.AvailableSeats); // при создании доступны все места
    }

    [Fact]
    public async Task CreateEvent_ShouldThrowValidationException_WhenTotalSeatsIsZero()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _eventService.CreateEventAsync(
                "Test Event", null,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(2),
                totalSeats: 0));
    }

    [Fact]
    public async Task CreateEvent_ShouldThrowValidationException_WhenTotalSeatsIsNegative()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _eventService.CreateEventAsync(
                "Test Event", null,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(2),
                totalSeats: -5));
    }

    [Fact]
    public async Task GetAllEvent_ShouldReturnAllEvents()
    {
        // Arrange
        var testEvents = TestData.GetSampleEvents(_eventService);

        // Act
        var result = await _eventService.GetAllEventAsync(null, null, null, 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testEvents.Count, result.TotalCount);
        Assert.Equal(testEvents.Count, result.Items.Count);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetEventById_ShouldReturnEvent_WhenEventExists()
    {
        // Arrange
        var testEvents = TestData.GetSampleEvents(_eventService);
        var expectedEvent = testEvents.First();

        // Act
        var result = await _eventService.GetByIdEventAsync(expectedEvent.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedEvent.Id, result.Id);
        Assert.Equal(expectedEvent.Title, result.Title);
        Assert.Equal(expectedEvent.Description, result.Description);
        Assert.Equal(expectedEvent.StartAt, result.StartAt);
        Assert.Equal(expectedEvent.EndAt, result.EndAt);
        Assert.Equal(expectedEvent.CreatedAt, result.CreatedAt);
    }

    [Fact]
    public async Task UpdateEvent_ShouldUpdateAndReturnEvent()
    {
        // Arrange
        var testEvents = TestData.GetSampleEvents(_eventService);
        var existingEvent = testEvents.First();

        var title = "Конференция .NET 22";
        var description = "Ежегодная конференция 22";
        var startAt = DateTime.UtcNow.AddDays(20);
        var endAt = DateTime.UtcNow.AddDays(21);

        // Act
        var result = await _eventService.UpdateEventAsync(existingEvent.Id, title, description, startAt, endAt);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(title, result.Title);
        Assert.Equal(description, result.Description);
        Assert.Equal(startAt.ToUniversalTime(), result.StartAt);
        Assert.Equal(endAt.ToUniversalTime(), result.EndAt);
    }

    [Fact]
    public async Task DeleteEvent_ShouldRemoveExistingEvent()
    {
        // Arrange
        var testEvents = TestData.GetSampleEvents(_eventService);
        var eventToDelete = testEvents.First();

        // Act
        await _eventService.DeleteEventAsync(eventToDelete.Id);

        // Assert
        var exception = await Assert.ThrowsAsync<EventNotFoundException>(() =>
            _eventService.GetByIdEventAsync(eventToDelete.Id));
        Assert.Equal(eventToDelete.Id, exception.EventId);
    }

    [Fact]
    public async Task FilterByTitle_ShouldReturnMatchingEvents()
    {
        // Arrange
        TestData.GetSampleEvents(_eventService);

        // Act
        var result = await _eventService.GetAllEventAsync("C#", null, null, 1, 10);

        // Assert
        Assert.Equal(2, result.Items.Count);
        foreach (var ev in result.Items)
        {
            Assert.Contains("C#", ev.Title);
            Assert.DoesNotContain("F#", ev.Title);
        }
    }

    [Fact]
    public async Task FilterByDateRange_ShouldReturnEventsWithinRange()
    {
        // Arrange
        TestData.GetDateRangeEvents(_eventService);
        var from = DateTime.UtcNow.Date;
        var to = DateTime.UtcNow.Date.AddDays(5);

        // Act
        var result = await _eventService.GetAllEventAsync(null, from, to, 1, 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Current Event", result.Items[0].Title);
    }

    [Fact]
    public async Task Pagination_ShouldReturnCorrectPage()
    {
        // Arrange
        const int totalEvents = 50;
        const int pageSize = 10;
        var expectedPages = (int)Math.Ceiling((double)totalEvents / pageSize);
        TestData.GetPaginationEvents(_eventService);

        // Act
        var page1 = await _eventService.GetAllEventAsync(null, null, null, 1, pageSize);
        var page2 = await _eventService.GetAllEventAsync(null, null, null, 2, pageSize);
        var page3 = await _eventService.GetAllEventAsync(null, null, null, 3, pageSize);
        var page4 = await _eventService.GetAllEventAsync(null, null, null, 4, pageSize);
        var page5 = await _eventService.GetAllEventAsync(null, null, null, 5, pageSize);

        // Assert
        Assert.Equal(pageSize, page1.Items.Count);
        Assert.Equal(pageSize, page2.Items.Count);
        Assert.Equal(pageSize, page3.Items.Count);
        Assert.Equal(pageSize, page4.Items.Count);
        Assert.Equal(pageSize, page5.Items.Count);
        Assert.Equal(totalEvents, page1.TotalCount);
        Assert.Equal(expectedPages, page1.TotalPages);
    }

    [Fact]
    public async Task CombinedFiltering_ShouldApplyAllFiltersTogether()
    {
        // Arrange
        TestData.GetSampleEvents(_eventService);
        var from = DateTime.UtcNow.Date.AddDays(2);
        var to = DateTime.UtcNow.Date.AddDays(7);

        // Act
        var result = await _eventService.GetAllEventAsync("C#", from, to, 1, 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("C# Conference", result.Items[0].Title);
    }

    // =============================================
    // НЕУСПЕШНЫЕ СЦЕНАРИИ
    // =============================================

    [Fact]
    public async Task GetByIdEvent_ShouldThrowEventNotFoundException_WhenEventDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EventNotFoundException>(() =>
            _eventService.GetByIdEventAsync(nonExistentId));
        Assert.Equal(nonExistentId, exception.EventId);
    }

    [Fact]
    public async Task UpdateEvent_ShouldThrowEventNotFoundException_WhenEventDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EventNotFoundException>(() =>
            _eventService.UpdateEventAsync(
                nonExistentId,
                "Title",
                null,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(2)));
        Assert.Equal(nonExistentId, exception.EventId);
    }

    [Fact]
    public async Task DeleteEvent_ShouldThrowEventNotFoundException_WhenEventDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EventNotFoundException>(() =>
            _eventService.DeleteEventAsync(nonExistentId));
        Assert.Equal(nonExistentId, exception.EventId);
    }

    [Fact]
    public async Task CreateEvent_ShouldThrowArgumentException_WhenTitleIsEmpty()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _eventService.CreateEventAsync(
                "", null,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(2),
                totalSeats: 10));

        Assert.Equal("Название события обязательно", exception.Message);
    }

    [Fact]
    public async Task CreateEvent_ShouldThrowArgumentException_WhenTitleIsTooLong()
    {
        // Arrange
        var longTitle = new string('A', 301);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _eventService.CreateEventAsync(
                longTitle, null,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(2),
                totalSeats: 10));

        Assert.Equal("Название не должно превышать 300 символов", exception.Message);
    }

    [Fact]
    public async Task CreateEvent_ShouldThrowArgumentException_WhenStartAtIsAfterEndAt()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _eventService.CreateEventAsync(
                "Test Event", null,
                DateTime.UtcNow.AddDays(5),
                DateTime.UtcNow.AddDays(2),
                totalSeats: 10));

        Assert.Equal("Дата начала должна быть раньше даты окончания", exception.Message);
    }

    [Fact]
    public async Task CreateEvent_ShouldThrowArgumentException_WhenStartAtIsInPast()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _eventService.CreateEventAsync(
                "Test Event", null,
                DateTime.UtcNow.AddDays(-5),
                DateTime.UtcNow.AddDays(-4),
                totalSeats: 10));

        Assert.Equal("Нельзя создавать событие в прошлом", exception.Message);
    }

    [Fact]
    public async Task UpdateEvent_ShouldThrowArgumentException_WhenEndAtIsBeforeStartAt()
    {
        // Arrange
        var testEvents = TestData.GetSampleEvents(_eventService);
        var existingEvent = testEvents.First();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _eventService.UpdateEventAsync(
                existingEvent.Id,
                "Updated Title",
                null,
                DateTime.UtcNow.AddDays(5),
                DateTime.UtcNow.AddDays(3)));

        Assert.Equal("Дата начала должна быть раньше даты окончания", exception.Message);
    }

    [Fact]
public async Task ConcurrentReadsAndWrites_ShouldNotThrow()
{
    // Arrange
    var writeTasks = Enumerable.Range(0, 50).Select(i =>
        _eventService.CreateEventAsync(
            $"Event {i}", null,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            totalSeats: 10));

    var readTasks = Enumerable.Range(0, 50).Select(_ =>
        _eventService.GetAllEventAsync(null, null, null, 1, 100));

    var allTasks = writeTasks.Cast<Task>().Concat(readTasks.Cast<Task>());
    // Act & Assert — не должно быть исключения
    await Task.WhenAll(allTasks);

    var result = await _eventService.GetAllEventAsync(null, null, null, 1, 100);
    Assert.Equal(50, result.TotalCount);
}
}