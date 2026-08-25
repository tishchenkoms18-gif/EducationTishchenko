/// <summary>
/// Доменная модель бронирования
/// </summary>
public class Booking {
    public Guid Id { get; private set; }  //уникальный идентификатор брони
    public Guid EventId { get; private set; }  //идентификатор события, к которому относится бронь;
    public BookingStatus Status { get; private set; } //текущий статус брони;
    public DateTime CreatedAt { get; private set; } //дата и время создания брони;
    public DateTime? ProcessedAt { get; private set; } //дата и время обработки брони.

    // Для EF Core (без параметров)
    private Booking(){}

     // Приватный конструктор с параметрами
    private Booking(
        Guid id,
        Guid eventId,
        BookingStatus status,
        DateTime createAt,
        DateTime processedAt
    )
    {
        Id = id;
        EventId = eventId;
        Status = status;
        CreatedAt = createAt; 
        ProcessedAt = processedAt; 
    }

    /// <summary>
    /// Создаёт новое бронирование со статусом Pending
    /// </summary>
    /// <param name="eventId">Идентификатор события</param>
    /// <returns>Новая бронь</returns>
    public static Booking Create(Guid eventId)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("EventId не может быть пустым", nameof(eventId));
       

        return new Booking
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
            
        };
    }

    // =============================================
    // Методы изменения статуса
    // =============================================

    /// <summary>
    /// Подтвердить бронь
    /// </summary>
    public void Confirm()
    {
        if (Status == BookingStatus.Confirmed)
            throw new InvalidOperationException("Бронь уже подтверждена");

        if (Status == BookingStatus.Rejected)
            throw new InvalidOperationException("Нельзя подтвердить отклонённую бронь");

        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Отклонить бронь
    /// </summary>
    public void Reject()
    {
        if (Status == BookingStatus.Rejected)
            throw new InvalidOperationException("Бронь уже отклонена");

        if (Status == BookingStatus.Confirmed)
            throw new InvalidOperationException("Нельзя отклонить подтверждённую бронь");

        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Проверить, можно ли обработать бронь
    /// </summary>
    public bool CanBeProcessed => Status == BookingStatus.Pending;

    /// <summary>
    /// Проверить, подтверждена ли бронь
    /// </summary>
    public bool IsConfirmed => Status == BookingStatus.Confirmed;

    /// <summary>
    /// Проверить, отклонена ли бронь
    /// </summary>
    public bool IsRejected => Status == BookingStatus.Rejected;

}