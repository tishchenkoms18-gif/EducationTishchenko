public class EventNotFoundException : Exception
{
    public Guid EventId { get; }

    public EventNotFoundException(Guid eventId)
        : base($"Событие с ID '{eventId}' не найдено.")
    {
        EventId = eventId;
    }

    public EventNotFoundException(Guid eventId, string message)
        : base(message)
    {
        EventId = eventId;
    }

    public EventNotFoundException(Guid eventId, string message, Exception innerException)
        : base(message, innerException)
    {
        EventId = eventId;
    }
}