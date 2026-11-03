/// <summary>
/// Интерфейс сервиса для работы с бронированиями
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Создать новое бронирование
    /// </summary>
    public  Task<Booking> CreateBookingAsync(Guid eventId);

     /// <summary>
    /// Получить бронирование по ID
    /// </summary>
    public  Task<Booking> GetBookingByIdAsync(Guid bookingId);

    /// <summary>
    /// Получить все бронирования для события
    /// </summary>
    Task<List<Booking>> GetBookingsByEventIdAsync(Guid eventId);
    /// <summary>
    /// Подтвердить бронирование
    /// </summary>
    Task ConfirmBookingAsync(Guid id);

    /// <summary>
    /// Отклонить бронирование
    /// </summary>
    Task RejectBookingAsync(Guid id);

    /// <summary>
    /// Получить все бронирования
    /// </summary>
    Task<List<Booking>> GetAllBookingsAsync();

    /// <summary>
    /// Получить бронирования по статусу
    /// </summary>
    Task<List<Booking>> GetBookingsByStatusAsync(BookingStatus status);
   
  /// <summary>
/// Обрабатывает бронирование, находящееся в статусе <see cref="BookingStatus.Pending"/>.
/// </summary>
/// <remarks>
/// Метод используется фоновым сервисом для асинхронной обработки заявок.
/// Логика:
/// <list type="number">
///   <item>Если бронь не найдена или уже обработана — ничего не делает.</item>
///   <item>Если связанное событие удалено — переводит бронь в статус <see cref="BookingStatus.Rejected"/> и возвращает место.</item>
///   <item>Если событие существует — переводит бронь в статус <see cref="BookingStatus.Confirmed"/>.</item>
/// </list>
/// </remarks>
/// <param name="bookingId">Идентификатор брони, которую нужно обработать.</param>
/// <param name="cancellationToken">Токен отмены операции.</param>
    Task ProcessBookingAsync(Guid bookingId, CancellationToken cancellationToken = default);
}
