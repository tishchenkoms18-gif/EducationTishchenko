using Microsoft.EntityFrameworkCore;

namespace WebAPI.DataAccess.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _db;
    public EventRepository(AppDbContext db)
    {
        _db = db;
    }

     public async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Events
        .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

     public async Task<List<Event>> GetAllAsync(
        string? title = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        
        
        // Пагинация
        return await BuildQuery(title, from, to)
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

     public async Task<int> CountAsync(
        string? title = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
       return await BuildQuery(title, from, to)
        .CountAsync(cancellationToken);
    }

     public async Task AddAsync(Event eventEntity, CancellationToken cancellationToken = default)
    {

      await _db.Events.AddAsync(eventEntity, cancellationToken);
    }

     public Task UpdateAsync(Event eventEntity, CancellationToken cancellationToken = default)
    {
        _db.Events.Update(eventEntity);
        return Task.CompletedTask;
    }

     public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _db.Events
        .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (eventEntity != null)
        {
            _db.Events.Remove(eventEntity);
        }
    }
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Event> BuildQuery(string? title, DateTime? from, DateTime? to)
{
    var query = _db.Events.AsNoTracking().AsQueryable();

    if (!string.IsNullOrWhiteSpace(title))
    {
        var titleLower = title.ToLower();
        query = query.Where(e => e.Title.ToLower().Contains(titleLower));
    }

    if (from.HasValue)
    {
        var fromUtc = from.Value.ToUniversalTime();
        query = query.Where(e => e.StartAt >= fromUtc);
    }

    if (to.HasValue)
    {
        var toUtc = to.Value.ToUniversalTime();
        query = query.Where(e => e.EndAt <= toUtc);
    }

    return query;
}
public async Task AddRangeAsync(IEnumerable<Event> eventEntities, CancellationToken cancellationToken = default)
{
    await _db.Events.AddRangeAsync(eventEntities, cancellationToken);
}
}
