/// <summary>
/// Ответ с данными о бронировании
/// </summary>
public class BookingResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}