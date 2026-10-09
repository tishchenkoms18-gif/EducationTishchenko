using Microsoft.EntityFrameworkCore;
using WebAPI.DataAccess;


public class EventService : IEventService
{
    private readonly AppDbContext _context;
    private readonly ILogger<EventService> _logger;

    public EventService(AppDbContext context, ILogger<EventService> logger)
    {
        _context = context;
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

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync(cancellationToken);

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

        _context.Events.AddRange(createdEvents);
        await _context.SaveChangesAsync(cancellationToken);

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
        var eventEntity = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new EventNotFoundException(id);

        eventEntity.Update(title, description, startAt, endAt);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Событие изменено: {Title} (ID: {Id})", eventEntity.Title, eventEntity.Id);
        return eventEntity;
    }

    public async Task DeleteEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new EventNotFoundException(id);

        _context.Events.Remove(eventEntity);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Удалено событие: {Title} (ID: {Id})", eventEntity.Title, eventEntity.Id);
    }

    public async Task<Event> GetByIdEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

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

        var query = _context.Events.AsQueryable();

        if (!string.IsNullOrWhiteSpace(title))
            query = query.Where(e => EF.Functions.ILike(e.Title, $"%{title}%"));

        if (from.HasValue)
            query = query.Where(e => e.StartAt >= from.Value.ToUniversalTime());

        if (to.HasValue)
            query = query.Where(e => e.EndAt <= to.Value.ToUniversalTime());

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

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