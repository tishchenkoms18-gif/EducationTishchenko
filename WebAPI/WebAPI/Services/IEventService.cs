/// <summary>
/// Контракт сервиса для управления мероприятиями
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Создать новое событие
    /// </summary>
    /// <param name="title"></param>
    /// <param name="description"></param>
    /// <param name="startAt"></param>
    /// <param name="endAt"></param>
    /// <param name="cancellationToken"></param>
    public Event CreateEvent(string title, string? description, DateTime startAt, DateTime endAt, CancellationToken cancellationToken = default);
    /// <summary>
    /// Обновить событие
    /// </summary>
    /// <param name="id"></param>
    /// <param name="title"></param>
    /// <param name="description"></param>
    /// <param name="startAt"></param>
    /// <param name="endAt"></param>
    public Event UpdateEvent(Guid id, string title, string? description, DateTime startAt, DateTime endAt);
    /// <summary>
    /// Удалить событие
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public bool DeleteEvent(Guid id);
    /// <summary>
    /// Найти событие по ид 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Event? GetByIdEvent(Guid id);
    /// <summary>
    /// Получить список всех событий
    /// </summary>
    /// <returns></returns>
    public PaginatedResult<Event> GetAllEvent(string? title = null , DateTime? from = null , DateTime? to = null, int page = 1, int pageSize = 10);
}
