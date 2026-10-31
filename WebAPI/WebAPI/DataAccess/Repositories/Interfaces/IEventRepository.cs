
public interface IEventRepository
{
    public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    public Task<List<Event>> GetAllAsync(
        string? title = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    public Task<int> CountAsync(
        string? title = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    public Task AddAsync(Event eventEntity, CancellationToken cancellationToken = default);

    public Task UpdateAsync(Event eventEntity, CancellationToken cancellationToken = default);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default);
    public Task AddRangeAsync(IEnumerable<Event> eventEntities, CancellationToken cancellationToken = default);
}