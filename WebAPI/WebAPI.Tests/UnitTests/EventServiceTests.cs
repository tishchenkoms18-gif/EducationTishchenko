using WebAPI;
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.AspNetCore.Mvc.ModelBinding;


namespace WebAPI.Tests.UnitTests.Services;

public class EventServiceTests
{
    private readonly EventService _eventService;
    private readonly ILogger<EventService> _logger;

    public EventServiceTests(){
         var loggerMock = new Mock<ILogger<EventService>>();
        _logger = loggerMock.Object;

        _eventService = new EventService(_logger);
    }


    // УСПЕШНЫЕ СЦЕНАРИИ
    [Fact]
    public void CreateEvent_ShouldCreateAndReturnEvent()
    {
        // Arrange
        var title = "Конференция .NET";
        var description = "Ежегодная конференция";
        var startAt = DateTime.UtcNow.AddDays(5);
        var endAt = DateTime.UtcNow.AddDays(6);
        
        // Act
        var result = _eventService.CreateEvent(title, description, startAt, endAt);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(title, result.Title);
        Assert.Equal(description, result.Description);
        Assert.Equal(startAt.ToUniversalTime(), result.StartAt);
        Assert.Equal(endAt.ToUniversalTime(), result.EndAt);
    }
    [Fact]
    public void GetAllEvent_ShouldReturnAllEvents()
    {
        // Arrange
        var testEvents = TestData.GetSampleEvents(_eventService);

        //Act
        var result = _eventService.GetAllEvent(null,null,null,1,10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testEvents.Count, result.TotalCount);  // Все события из TestData
        Assert.Equal(testEvents.Count, result.Items.Count);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public void GetEventById_ShouldReturnEvent_WhenEventExists()
    {
        // Arrange
        var testEvents = TestData.GetSampleEvents(_eventService);
        
        var expectedEvent = testEvents.First();
        var expectedId = expectedEvent.Id;

        // Act
        var result = _eventService.GetByIdEvent(expectedId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedId, result.Id);
        Assert.Equal(expectedEvent.Title, result.Title);
        Assert.Equal(expectedEvent.Description, result.Description);
        Assert.Equal(expectedEvent.StartAt, result.StartAt);
        Assert.Equal(expectedEvent.EndAt, result.EndAt);
        Assert.Equal(expectedEvent.CreatedAt, result.CreatedAt);
    }

    [Fact]
    public void UpdateEvent_ShouldCreateAndReturnEvent()
    {
        // Arrange
        var testEvents = TestData.GetSampleEvents(_eventService);
        var existingEvent = testEvents.First();

        var title = "Конференция .NET 22";
        var description = "Ежегодная конференция 22";
        var startAt = DateTime.UtcNow.AddDays(20);
        var endAt = DateTime.UtcNow.AddDays(21);


        //Act
        var result = _eventService.UpdateEvent(existingEvent.Id, title, description, startAt, endAt);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(title, result.Title);
        Assert.Equal(description, result.Description);
        Assert.Equal(startAt.ToUniversalTime(), result.StartAt);
        Assert.Equal(endAt.ToUniversalTime(), result.EndAt);
    }

    [Fact]
    public void DeleteEvent_ShouldRemoveExistingEvent()
    {
         // Arrange
        var testEvents = TestData.GetSampleEvents(_eventService);
        var eventToDelete = testEvents.First();

        var result = _eventService.DeleteEvent(eventToDelete.Id);

        // Assert
        Assert.True(result);
        
        var exception = Assert.Throws<EventNotFoundException>(() =>
        _eventService.GetByIdEvent(eventToDelete.Id));
    
        Assert.Equal(eventToDelete.Id, exception.EventId);
    }

    [Fact]
    public void FilterByTitle_ShouldReturnMatchingEvents()
    {
        // Arrange
        TestData.GetSampleEvents(_eventService);

        // Act
        var result = _eventService.GetAllEvent("C#", null, null, 1, 10);

        // Assert
        Assert.Equal(2, result.Items.Count);
        foreach (var ev in result.Items)
        {
            Assert.Contains("C#", ev.Title);
            Assert.DoesNotContain("F#", ev.Title);
        }
    }

    [Fact]
    public void FilterByDateRange_ShouldReturnEventsWithinRange()
    {
        // Arrange
        TestData.GetDateRangeEvents(_eventService);
        var from = DateTime.UtcNow.Date;
        var to = DateTime.UtcNow.Date.AddDays(5);

        // Act
        var result = _eventService.GetAllEvent(null, from, to, 1, 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Current Event", result.Items[0].Title);
    }

    [Fact]
    public void Pagination_ShouldReturnCorrectPage()
    {
        // Arrange
        const int totalEvents = 50;
        const int pageSize = 10;
        var expectedPages = (int)Math.Ceiling((double)totalEvents / pageSize); // 5
        TestData.GetPaginationEvents(_eventService);

        // Act
        var page1 = _eventService.GetAllEvent(null, null, null, 1, pageSize);
        var page2 = _eventService.GetAllEvent(null, null, null, 2, pageSize);
        var page3 = _eventService.GetAllEvent(null, null, null, 3, pageSize);
        var page4 = _eventService.GetAllEvent(null, null, null, 4, pageSize);
        var page5 = _eventService.GetAllEvent(null, null, null, 5, pageSize);

        // Assert
        Assert.Equal(pageSize, page1.Items.Count);
        Assert.Equal(pageSize, page5.Items.Count);
        Assert.Equal(pageSize, page2.Items.Count);
        Assert.Equal(pageSize, page3.Items.Count);
        Assert.Equal(pageSize, page4.Items.Count);
    
        // Общее количество совпадает
        Assert.Equal(totalEvents, page1.TotalCount);
    
        // Общее количество страниц совпадает
        Assert.Equal(expectedPages, page1.TotalPages);
    }

    [Fact]
    public void CombinedFiltering_ShouldApplyAllFiltersTogether()
    {
        // Arrange
        TestData.GetSampleEvents(_eventService);
        var from = DateTime.UtcNow.Date.AddDays(2);
        var to = DateTime.UtcNow.Date.AddDays(7);

        // Act
        var result = _eventService.GetAllEvent("C#", from, to, 1, 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("C# Conference", result.Items[0].Title);
    }

    // НЕУСПЕШНЫЕ СЦЕНАРИИ

    [Fact]
    public void GetByIdEvent_ShouldThrowKeyNotFoundException_WhenEventDoesNotExist()
    {
        // Arrange
       var nonExistentId = Guid.NewGuid();

       var exception = Assert.Throws<EventNotFoundException>(() =>
            _eventService.GetByIdEvent(nonExistentId));
        
        Assert.Equal($"Событие с ID '{nonExistentId}' не найдено.", exception.Message);

    }

    [Fact]
    public void DeleteEvent_ShouldReturnFalse_WhenEventDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = _eventService.DeleteEvent(nonExistentId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CreateEvent_ShouldThrowArgumentException_WhenTitleIsEmpty()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _eventService.CreateEvent("", null, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2)));
        
        Assert.Equal("Название события обязательно", exception.Message);
    }

    [Fact]
    public void CreateEvent_ShouldThrowArgumentException_WhenTitleIsTooLong()
    {
        // Arrange
        var longTitle = new string('A', 301);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _eventService.CreateEvent(longTitle, null, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2)));
        
        Assert.Equal("Название не должно превышать 300 символов", exception.Message);
    }

    [Fact]
    public void CreateEvent_ShouldThrowArgumentException_WhenStartAtIsAfterEndAt()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _eventService.CreateEvent(
                "Test Event", 
                null, 
                DateTime.UtcNow.AddDays(5), 
                DateTime.UtcNow.AddDays(2)));
        
        Assert.Equal("Дата начала должна быть раньше даты окончания", exception.Message);
    }

    [Fact]
    public void CreateEvent_ShouldThrowArgumentException_WhenStartAtIsInPast()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _eventService.CreateEvent(
                "Test Event", 
                null, 
                DateTime.UtcNow.AddDays(-5), 
                DateTime.UtcNow.AddDays(-4)));
        
        Assert.Equal("Нельзя создавать событие в прошлом", exception.Message);
    }

    [Fact]
    public void UpdateEvent_ShouldThrowArgumentException_WhenEndAtIsBeforeStartAt()
    {
        // Arrange
        var testEvents = TestData.GetSampleEvents(_eventService);
        var existingEvent = testEvents.First();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _eventService.UpdateEvent(
                existingEvent.Id,
                "Updated Title",
                null,
                DateTime.UtcNow.AddDays(5),
                DateTime.UtcNow.AddDays(3)));
        
        Assert.Equal("Дата начала должна быть раньше даты окончания", exception.Message);
    }
}
