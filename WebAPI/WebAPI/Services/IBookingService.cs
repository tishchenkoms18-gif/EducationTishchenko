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
}
