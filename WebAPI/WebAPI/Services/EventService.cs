public class EventService : IEventService
{
    private readonly List<Event> _events = new();
    private readonly ILogger<EventService> _logger;

    public EventService(ILogger<EventService> logger)
    {
        _logger = logger;
    }

    public Event CreateEvent(string title, string? description, DateTime startAt, DateTime endAt, CancellationToken cancellationToken = default)
    {
        var eventEntity = Event.Create(title, description, startAt, endAt);
        _events.Add(eventEntity);
        _logger.LogInformation("Создано событие: {Title} (ID: {Id})", eventEntity.Title, eventEntity.Id);
        return eventEntity;
    }
    public List<Event> CreateEvents(List<CreateEventRequest> eventRequests)
    {
        if (eventRequests == null || !eventRequests.Any())
            throw new ArgumentException("Список событий не может быть пустым");
        var createdEvents = new List<Event>();

        foreach (var request in eventRequests)
        {
            try
            {
                var eventEntity = Event.Create(
                    request.Title,
                    request.Description,
                    request.StartAt,
                    request.EndAt);

                _events.Add(eventEntity);
                createdEvents.Add(eventEntity);
            }
            catch (ArgumentException ex)
            {
                // Можно либо пропускать ошибочные, либо прерывать весь процесс
                // Здесь мы выбрасываем исключение с информацией о том, какое событие вызвало проблему
                throw new ArgumentException($"Ошибка при создании события '{request.Title}': {ex.Message}", ex);
            }
        }

        _logger.LogInformation("Создано {Count} событий за один запрос", createdEvents.Count);

        return createdEvents; 

    }
    public Event UpdateEvent(Guid id, string title, string? description, DateTime startAt, DateTime endAt)
    {
         var eventEntity = _events.FirstOrDefault(e => e.Id == id) ??  throw new KeyNotFoundException();
        _logger.LogInformation("Событие изменено: {Title} (ID: {Id})", eventEntity.Title, eventEntity.Id);
        eventEntity.Update(title, description, startAt, endAt);
        return eventEntity;
    }
     public bool DeleteEvent(Guid id)
    {
         var eventEntity = _events.FirstOrDefault(e => e.Id == id);
        if (eventEntity == null)
            return false;

        _events.Remove(eventEntity);
        _logger.LogInformation("Удалено событие: {Title} (ID: {Id})", eventEntity.Title, eventEntity.Id);
        return true;
    }

     public Event? GetByIdEvent(Guid id){
        return _events.FirstOrDefault(e => e.Id == id);
     }
    public PaginatedResult<Event> GetAllEvent(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10 )
    {
        
       var query =  _events.OrderByDescending(e => e.StartAt).AsQueryable();

        if (!string.IsNullOrWhiteSpace(title))
        {
            query = query.Where(e => e.Title.Contains(title, StringComparison.CurrentCultureIgnoreCase));
        }
        if (from.HasValue)
        {
            query = query.Where(e => e.StartAt >= from.Value.ToUniversalTime());
        }
        if (to.HasValue)
        {
             query = query.Where(e => e.StartAt <= to.Value.ToUniversalTime());
        }

        // Сортировка и пагинация
    // Сортировка и пагинация
    var totalCount = query.Count();
    var items = query
        .OrderByDescending(e => e.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToList();

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