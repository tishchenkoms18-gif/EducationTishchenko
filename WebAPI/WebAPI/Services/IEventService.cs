

namespace WebAPI.Services;

/// <summary>
/// Контракт сервиса для управления мероприятиями
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Создать новое событие
    /// </summary>
    Task<Event> CreateEventAsync(
        string title,
        string? description,
        DateTime startAt,
        DateTime endAt,
        int totalSeats,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Создание нескольких событий
    /// </summary>
    Task<List<Event>> CreateEventsAsync(
        List<CreateEventRequest> eventRequests,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновить событие
    /// </summary>
    Task<Event> UpdateEventAsync(
        Guid id,
        string title,
        string? description,
        DateTime startAt,
        DateTime endAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Удалить событие
    /// </summary>
    Task DeleteEventAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Найти событие по Id
    /// </summary>
    Task<Event> GetByIdEventAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить список всех событий с фильтрацией и пагинацией
    /// </summary>
    Task<PaginatedResult<Event>> GetAllEventAsync(
        string? title = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
}