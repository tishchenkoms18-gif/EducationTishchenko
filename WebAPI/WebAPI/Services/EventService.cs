using WebAPI.Services;

public class EventService : IEventService
{
    private readonly List<Event> _events = new();
    private readonly ILogger<EventService> _logger;
    private readonly object _eventsLock = new(); // 🔒 для потокобезопасности

    public EventService(ILogger<EventService> logger)
    {
        _logger = logger;
    }

    public Task<Event> CreateEventAsync(
        string title,
        string? description,
        DateTime startAt,
        DateTime endAt,
        int totalSeats,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = Event.Create(title, description, startAt, endAt, totalSeats);

        lock (_eventsLock)
        {
            _events.Add(eventEntity);
        }

        _logger.LogInformation(
            "Создано событие: {Title} (ID: {Id}), мест: {TotalSeats}",
            eventEntity.Title,
            eventEntity.Id,
            eventEntity.TotalSeats);

        return Task.FromResult(eventEntity);
    }

    public Task<List<Event>> CreateEventsAsync(
        List<CreateEventRequest> eventRequests,
        CancellationToken cancellationToken = default)
    {
        if (eventRequests == null || !eventRequests.Any())
            throw new ArgumentException("Список событий не может быть пустым");

        var createdEvents = new List<Event>();

        foreach (var request in eventRequests)
        {
            var eventEntity = Event.Create(
                request.Title,
                request.Description,
                request.StartAt,
                request.EndAt,
                request.TotalSeats);

            createdEvents.Add(eventEntity);
        }

        lock (_eventsLock)
        {
            _events.AddRange(createdEvents);
        }

        _logger.LogInformation("Создано {Count} событий за один запрос", createdEvents.Count);
        return Task.FromResult(createdEvents);
    }

    public Task<Event> UpdateEventAsync(
        Guid id,
        string title,
        string? description,
        DateTime startAt,
        DateTime endAt,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = _events.FirstOrDefault(e => e.Id == id)
            ?? throw new EventNotFoundException(id);

        eventEntity.Update(title, description, startAt, endAt);

        _logger.LogInformation("Событие изменено: {Title} (ID: {Id})", eventEntity.Title, eventEntity.Id);
        return Task.FromResult(eventEntity);
    }

    public Task DeleteEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_eventsLock)
        {
            var eventEntity = _events.FirstOrDefault(e => e.Id == id)
                ?? throw new EventNotFoundException(id);

            _events.Remove(eventEntity);
            _logger.LogInformation("Удалено событие: {Title} (ID: {Id})", eventEntity.Title, eventEntity.Id);
        }

        return Task.CompletedTask;
    }

    public Task<Event> GetByIdEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var eventEntity = _events.FirstOrDefault(e => e.Id == id);
        if (eventEntity == null)
            throw new EventNotFoundException(id);

        return Task.FromResult(eventEntity);
    }

    public Task<PaginatedResult<Event>> GetAllEventAsync(
        string? title = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
            throw new ValidationException("Page must be greater than 0");

        if (pageSize < 1 || pageSize > 100)
            throw new ValidationException("PageSize must be between 1 and 100");

        var query = _events.AsQueryable();

        if (!string.IsNullOrWhiteSpace(title))
            query = query.Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));

        if (from.HasValue)
            query = query.Where(e => e.StartAt >= from.Value.ToUniversalTime());

        if (to.HasValue)
            query = query.Where(e => e.EndAt <= to.Value.ToUniversalTime());

        var totalCount = query.Count();
        var items = query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new PaginatedResult<Event>
        {
            TotalCount = totalCount,
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };

        return Task.FromResult(result);
    }
}