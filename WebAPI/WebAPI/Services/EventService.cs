using Microsoft.EntityFrameworkCore;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<EventService> _logger;

    public EventService(IEventRepository eventRepository, ILogger<EventService> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<Event> CreateEventAsync(
        string title,
        string? description,
        DateTime startAt,
        DateTime endAt,
        int totalSeats,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = Event.Create(title, description, startAt, endAt, totalSeats);

        await _eventRepository.AddAsync(eventEntity, cancellationToken);
        await _eventRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Создано событие: {Title} (ID: {Id}), мест: {TotalSeats}",
            eventEntity.Title, eventEntity.Id, eventEntity.TotalSeats);

        return eventEntity;
    }

    public async Task<List<Event>> CreateEventsAsync(
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

        await _eventRepository.AddRangeAsync(createdEvents, cancellationToken);
        await _eventRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Создано {Count} событий за один запрос", createdEvents.Count);
        return createdEvents;
    }

    public async Task<Event> UpdateEventAsync(
        Guid id,
        string title,
        string? description,
        DateTime startAt,
        DateTime endAt,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new EventNotFoundException(id);

        eventEntity.Update(title, description, startAt, endAt);
        
        await _eventRepository.UpdateAsync(eventEntity, cancellationToken);
        await _eventRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Событие изменено: {Title} (ID: {Id})", eventEntity.Title, eventEntity.Id);
        return eventEntity;
    }

    public async Task DeleteEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new EventNotFoundException(id);

        await _eventRepository.DeleteAsync(id, cancellationToken);
        await _eventRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Удалено событие: {Title} (ID: {Id})", eventEntity.Title, eventEntity.Id);
    }

    public async Task<Event> GetByIdEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(id, cancellationToken);

        if (eventEntity == null)
            throw new EventNotFoundException(id);

        return eventEntity;
    }

    public async Task<PaginatedResult<Event>> GetAllEventAsync(
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

        var totalCount = await _eventRepository.CountAsync(title, from, to, cancellationToken);
        
        var items = await _eventRepository.GetAllAsync(title, from, to, page, pageSize, cancellationToken);

        return new PaginatedResult<Event>
        {
            TotalCount = totalCount,
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}