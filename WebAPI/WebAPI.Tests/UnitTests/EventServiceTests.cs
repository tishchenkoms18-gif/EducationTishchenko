// WebAPI.Tests/UnitTests/Services/EventServiceTests.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebAPI.DataAccess;
using Xunit;

namespace WebAPI.Tests.UnitTests.Services;

public class EventServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public EventServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddScoped<IEventService, EventService>();
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose() => _serviceProvider.Dispose();

    // =============================================
    // ХЕЛПЕР
    // =============================================

    private static IEventService GetEventService(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<IEventService>();

    // =============================================
    // УСПЕШНЫЕ СЦЕНАРИИ
    // =============================================

    [Fact]
    public async Task CreateEvent_ShouldCreateAndReturnEvent()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = GetEventService(scope);

        // Arrange
        var title = "Конференция .NET";
        var description = "Ежегодная конференция";
        var startAt = DateTime.UtcNow.AddDays(5);
        var endAt = DateTime.UtcNow.AddDays(6);
        var totalSeats = 100;

        // Act
        var result = await eventService.CreateEventAsync(
            title, description, startAt, endAt, totalSeats);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(title, result.Title);
        Assert.Equal(description, result.Description);
        Assert.Equal(startAt.ToUniversalTime(), result.StartAt);
        Assert.Equal(endAt.ToUniversalTime(), result.EndAt);
        Assert.Equal(totalSeats, result.TotalSeats);
        Assert.Equal(totalSeats, result.AvailableSeats);
    }

    [Fact]
    public async Task CreateEvent_ShouldThrowValidationException_WhenTotalSeatsIsZero()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = GetEventService(scope);

        await Assert.ThrowsAsync<ValidationException>(() =>
            eventService.CreateEventAsync(
                "Test Event", null,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(2),
                totalSeats: 0));
    }

    [Fact]
    public async Task CreateEvent_ShouldThrowValidationException_WhenTotalSeatsIsNegative()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = GetEventService(scope);

        await Assert.ThrowsAsync<ValidationException>(() =>
            eventService.CreateEventAsync(
                "Test Event", null,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(2),
                totalSeats: -5));
    }

    [Fact]
    public async Task GetAllEvent_ShouldReturnAllEvents()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = GetEventService(scope);

        // Arrange
        await CreateSampleEventsAsync(eventService);

        // Act
        var result = await eventService.GetAllEventAsync(null, null, null, 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(5, result.Items.Count);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetEventById_ShouldReturnEvent_WhenEventExists()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = GetEventService(scope);

        // Arrange
        var created = await eventService.CreateEventAsync(
            "Test Event", "Desc",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            50);

        // Act
        var result = await eventService.GetByIdEventAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Test Event", result.Title);
        Assert.Equal("Desc", result.Description);
        Assert.Equal(50, result.TotalSeats);
        Assert.Equal(50, result.AvailableSeats);
    }

    [Fact]
    public async Task UpdateEvent_ShouldUpdateAndReturnEvent()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = GetEventService(scope);

        // Arrange
        var created = await eventService.CreateEventAsync(
            "Old Title", null,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            10);

        var newStart = DateTime.UtcNow.AddDays(20);
        var newEnd = DateTime.UtcNow.AddDays(21);

        // Act
        var result = await eventService.UpdateEventAsync(
            created.Id, "New Title", "New Desc", newStart, newEnd);

        // Assert
        Assert.Equal("New Title", result.Title);
        Assert.Equal("New Desc", result.Description);
        Assert.Equal(newStart.ToUniversalTime(), result.StartAt);
        Assert.Equal(newEnd.ToUniversalTime(), result.EndAt);
    }

    [Fact]
    public async Task DeleteEvent_ShouldRemoveExistingEvent()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = GetEventService(scope);

        // Arrange
        var created = await eventService.CreateEventAsync(
            "To Delete", null,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            10);

        // Act
        await eventService.DeleteEventAsync(created.Id);

        // Assert
        var exception = await Assert.ThrowsAsync<EventNotFoundException>(() =>
            eventService.GetByIdEventAsync(created.Id));
        Assert.Equal(created.Id, exception.EventId);
    }

    [Fact]
    public async Task FilterByTitle_ShouldReturnMatchingEvents()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = GetEventService(scope);

        // Arrange
        await eventService.CreateEventAsync("C# Workshop", "Programming",
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
        await eventService.CreateEventAsync("C# Conference", "Programming",
            DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(6), 50);
        await eventService.CreateEventAsync("F# Workshop", "Programming",
            DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(4), 20);

        // Act
        var result = await eventService.GetAllEventAsync("C#", null, null, 1, 10);

        // Assert
        Assert.Equal(2, result.Items.Count);
        foreach (var ev in result.Items)
        {
            Assert.Contains("C#", ev.Title);
        }
    }

    [Fact]
    public async Task FilterByDateRange_ShouldReturnEventsWithinRange()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = GetEventService(scope);

        // Arrange
        var baseDate = DateTime.UtcNow.Date;
        await eventService.CreateEventAsync("Past Event", null,
            baseDate.AddDays(1), baseDate.AddDays(2), 10);
        await eventService.CreateEventAsync("Current Event", null,
            baseDate.AddDays(3), baseDate.AddDays(4), 10);
        await eventService.CreateEventAsync("Future Event", null,
            baseDate.AddDays(10), baseDate.AddDays(11), 10);

        var from = baseDate.AddDays(2);
        var to = baseDate.AddDays(5);

        // Act
        var result = await eventService.GetAllEventAsync(null, from, to, 1, 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Current Event", result.Items[0].Title);
    }

    [Fact]
    public async Task Pagination_ShouldReturnCorrectPage()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = GetEventService(scope);

        // Arrange
        const int totalEvents = 50;
        const int pageSize = 10;

        for (int i = 1; i <= totalEvents; i++)
        {
            await eventService.CreateEventAsync(
                $"Event {i}", null,
                DateTime.UtcNow.AddDays(i), DateTime.UtcNow.AddDays(i + 1), 100);
        }

        // Act
        var page1 = await eventService.GetAllEventAsync(null, null, null, 1, pageSize);
        var page5 = await eventService.GetAllEventAsync(null, null, null, 5, pageSize);

        // Assert
        Assert.Equal(pageSize, page1.Items.Count);
        Assert.Equal(pageSize, page5.Items.Count);
        Assert.Equal(totalEvents, page1.TotalCount);
        Assert.Equal(5, page1.TotalPages);
    }

    // =============================================
    // НЕУСПЕШНЫЕ СЦЕНАРИИ
    // =============================================

    [Fact]
    public async Task GetByIdEvent_ShouldThrowEventNotFoundException_WhenEventDoesNotExist()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = GetEventService(scope);

        var nonExistentId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<EventNotFoundException>(() =>
            eventService.GetByIdEventAsync(nonExistentId));
        Assert.Equal(nonExistentId, exception.EventId);
    }

    [Fact]
    public async Task UpdateEvent_ShouldThrowEventNotFoundException_WhenEventDoesNotExist()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = GetEventService(scope);

        var nonExistentId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<EventNotFoundException>(() =>
            eventService.UpdateEventAsync(
                nonExistentId, "Title", null,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(2)));
        Assert.Equal(nonExistentId, exception.EventId);
    }

    [Fact]
    public async Task DeleteEvent_ShouldThrowEventNotFoundException_WhenEventDoesNotExist()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = GetEventService(scope);

        var nonExistentId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<EventNotFoundException>(() =>
            eventService.DeleteEventAsync(nonExistentId));
        Assert.Equal(nonExistentId, exception.EventId);
    }

    [Fact]
    public async Task CreateEvent_ShouldThrowArgumentException_WhenTitleIsEmpty()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = GetEventService(scope);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            eventService.CreateEventAsync(
                "", null,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(2),
                totalSeats: 10));

        Assert.Equal("Название события обязательно", exception.Message);
    }

    [Fact]
    public async Task CreateEvent_ShouldThrowArgumentException_WhenStartAtIsAfterEndAt()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = GetEventService(scope);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            eventService.CreateEventAsync(
                "Test Event", null,
                DateTime.UtcNow.AddDays(5),
                DateTime.UtcNow.AddDays(2),
                totalSeats: 10));

        Assert.Equal("Дата начала должна быть раньше даты окончания", exception.Message);
    }

    // =============================================
    // ХЕЛПЕР: создание набора событий
    // =============================================

    private static async Task CreateSampleEventsAsync(IEventService eventService)
    {
        await eventService.CreateEventAsync("C# Workshop", "Programming",
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
        await eventService.CreateEventAsync("C# Conference", "Programming",
            DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(6), 50);
        await eventService.CreateEventAsync("F# Workshop", "Programming",
            DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(4), 20);
        await eventService.CreateEventAsync("ASP.NET Core", "Web",
            DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(11), 30);
        await eventService.CreateEventAsync("Azure DevOps", "DevOps",
            DateTime.UtcNow.AddDays(15), DateTime.UtcNow.AddDays(16), 40);
    }
}