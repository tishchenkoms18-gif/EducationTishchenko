// WebAPI.IntegrationTests/EventRepositoryTests.cs
using Microsoft.EntityFrameworkCore;
using WebAPI.IntegrationTests.Fixtures;
using Xunit;
using WebAPI.Infrastructure.DataAccess.Repositories;

namespace WebAPI.IntegrationTests;

public class EventRepositoryTests : BaseRepositoryTest
{
    private readonly EventRepository _repository;

    public EventRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
        _repository = new EventRepository(DbContext);
    }

    // Вспомогательный метод для создания события
    private Event CreateSampleEvent(string title = "Test Event", int daysFromNow = 1)
    {
        return Event.Create(
            title, "Description",
            DateTime.UtcNow.AddDays(daysFromNow),
            DateTime.UtcNow.AddDays(daysFromNow + 1),
            100);
    }

    [Fact]
    public async Task AddAsync_ShouldSaveEventToDatabase()
    {
        // Arrange
        var eventEntity = CreateSampleEvent();

        // Act
        await _repository.AddAsync(eventEntity);
        await _repository.SaveChangesAsync();

        DbContext.ChangeTracker.Clear();
        // Assert
        var saved = await DbContext.Events.FindAsync(eventEntity.Id);
        Assert.NotNull(saved);
        Assert.Equal(eventEntity.Title, saved.Title);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnEvent_WhenExists()
    {
        // Arrange
        var eventEntity = CreateSampleEvent();
        await _repository.AddAsync(eventEntity);
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(eventEntity.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(eventEntity.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyEvent()
    {
        // Arrange
        var eventEntity = CreateSampleEvent();
        await _repository.AddAsync(eventEntity);
        await _repository.SaveChangesAsync();
        
        DbContext.ChangeTracker.Clear();
        // Act
        eventEntity.Update("Updated Title", "New Desc", DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(6));
        await _repository.UpdateAsync(eventEntity);
        await _repository.SaveChangesAsync();

        // Assert
        var updated = await _repository.GetByIdAsync(eventEntity.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated Title", updated.Title);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveEvent()
    {
        // Arrange
        var eventEntity = CreateSampleEvent();
        await _repository.AddAsync(eventEntity);
        await _repository.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(eventEntity.Id);
        await _repository.SaveChangesAsync();

        // Assert
        var deleted = await _repository.GetByIdAsync(eventEntity.Id);
        Assert.Null(deleted);
    }

   

    // =============================================
    // ТЕСТЫ ФИЛЬТРАЦИИ И ПАГИНАЦИИ (GetAllAsync / CountAsync)
    // =============================================

    [Fact]
    public async Task GetAllAsync_ShouldReturnPaginatedEvents()
    {
        // Arrange
        for (int i = 1; i <= 15; i++)
        {
            await _repository.AddAsync(CreateSampleEvent($"Event {i}"));
        }
        await _repository.SaveChangesAsync();

        // Act
        var page1 = await _repository.GetAllAsync(page: 1, pageSize: 10);
        var page2 = await _repository.GetAllAsync(page: 2, pageSize: 10);
        var totalCount = await _repository.CountAsync();

        // Assert
        Assert.Equal(10, page1.Count);
        Assert.Equal(5, page2.Count);
        Assert.Equal(15, totalCount);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByTitle()
    {
        // Arrange
        await _repository.AddAsync(CreateSampleEvent("C# Workshop"));
        await _repository.AddAsync(CreateSampleEvent("C# Conference"));
        await _repository.AddAsync(CreateSampleEvent("F# Workshop"));
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync(title: "C#");
        var count = await _repository.CountAsync(title: "C#");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(2, count);
        Assert.All(result, e => Assert.Contains("C#", e.Title));
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByDateRange()
    {
        // Arrange
        var baseDate = DateTime.UtcNow.Date;
        
        // Событие 1: начало через 2 дня, конец через 3 дня
        var e1 = Event.Create("Event 1", null, baseDate.AddDays(2), baseDate.AddDays(3), 10);
        // Событие 2: начало через 10 дней, конец через 11 дней
        var e2 = Event.Create("Event 2", null, baseDate.AddDays(10), baseDate.AddDays(11), 10);
        // Событие 3: начало через 20 дней, конец через 21 день
        var e3 = Event.Create("Event 3", null, baseDate.AddDays(20), baseDate.AddDays(21), 10);

        await _repository.AddAsync(e1);
        await _repository.AddAsync(e2);
        await _repository.AddAsync(e3);
        await _repository.SaveChangesAsync();

        // Act: ищем события с StartAt >= 5 дней и EndAt <= 15 дней
        var result = await _repository.GetAllAsync(from: baseDate.AddDays(5), to: baseDate.AddDays(15));
        var count = await _repository.CountAsync(from: baseDate.AddDays(5), to: baseDate.AddDays(15));

        // Assert
        Assert.Single(result);
        Assert.Equal("Event 2", result[0].Title);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetAllAsync_ShouldApplyCombinedFilters()
    {
        // Arrange
        var baseDate = DateTime.UtcNow.Date;
        await _repository.AddAsync(Event.Create("C# Workshop", null, baseDate.AddDays(1), baseDate.AddDays(2), 10));
        await _repository.AddAsync(Event.Create("C# Conference", null, baseDate.AddDays(5), baseDate.AddDays(6), 10));
        await _repository.AddAsync(Event.Create("F# Workshop", null, baseDate.AddDays(3), baseDate.AddDays(4), 10));
        await _repository.SaveChangesAsync();

        // Act: ищем "C#" в диапазоне от 4 до 7 дней
        var result = await _repository.GetAllAsync(
            title: "C#", 
            from: baseDate.AddDays(4), 
            to: baseDate.AddDays(7));

        // Assert
        Assert.Single(result);
        Assert.Equal("C# Conference", result[0].Title);
    }

        [Fact]
    public async Task AddRangeAsync_ShouldSaveMultipleEvents()
    {
        // Arrange
        var events = new List<Event>
        {
            CreateSampleEvent("Event 1"),
            CreateSampleEvent("Event 2")
        };

        // Act
        await _repository.AddRangeAsync(events);
        await _repository.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        // Assert
        var count = await DbContext.Events.CountAsync();
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByFromOnly()
    {
        // Arrange
        var baseDate = DateTime.UtcNow.Date;
        await _repository.AddAsync(Event.Create("E1", null, baseDate.AddDays(1), baseDate.AddDays(2), 10));
        await _repository.AddAsync(Event.Create("E2", null, baseDate.AddDays(10), baseDate.AddDays(11), 10));
        await _repository.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetAllAsync(from: baseDate.AddDays(5));

        // Assert
        Assert.Single(result);
        Assert.Equal("E2", result[0].Title);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByToOnly()
    {
        // Arrange
        var baseDate = DateTime.UtcNow.Date;
        await _repository.AddAsync(Event.Create("E1", null, baseDate.AddDays(1), baseDate.AddDays(2), 10));
        await _repository.AddAsync(Event.Create("E2", null, baseDate.AddDays(10), baseDate.AddDays(11), 10));
        await _repository.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetAllAsync(to: baseDate.AddDays(5));

        // Assert
        Assert.Single(result);
        Assert.Equal("E1", result[0].Title);
    }

    [Fact]
    public async Task GetAllAsync_ShouldBeCaseInsensitive_WhenFilteringByTitle()
    {
        // Arrange
        await _repository.AddAsync(CreateSampleEvent("C# Workshop"));
        await _repository.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetAllAsync(title: "c#"); // Маленькими буквами

        // Assert
        Assert.Single(result);
        Assert.Equal("C# Workshop", result[0].Title);
    }

    [Fact]
    public async Task GetAllAsync_ShouldOrderByCreatedAtDescending()
    {
        // Arrange
        var e1 = CreateSampleEvent("First");
        await _repository.AddAsync(e1);
        await _repository.SaveChangesAsync();
        
        await Task.Delay(10); // Небольшая задержка, чтобы CreatedAt отличался
        
        var e2 = CreateSampleEvent("Second");
        await _repository.AddAsync(e2);
        await _repository.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Equal("Second", result[0].Title); // Сначала новые
        Assert.Equal("First", result[1].Title);
    }
}