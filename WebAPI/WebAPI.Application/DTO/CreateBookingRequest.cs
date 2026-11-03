/// <summary>
/// Запрос на создание бронирования
/// </summary>
public class CreateBookingRequest
{
    /// <summary>
    /// Идентификатор события
    /// </summary>
    public Guid EventId { get; set; }
}