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

    public Event UpdateEvent(Guid id, string title, string? description, DateTime startAt, DateTime endAt)
    {
         var eventEntity = _events.FirstOrDefault(e => e.Id == id) ??  throw new KeyNotFoundException();
        _logger.LogInformation("Создано изменено: {Title} (ID: {Id})", eventEntity.Title, eventEntity.Id);
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
    public List<Event> GetAllEvent()
    {
        return _events.OrderByDescending(e => e.StartAt).ToList();
    }
}