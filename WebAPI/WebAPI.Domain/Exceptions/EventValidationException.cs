public class EventValidationException : Exception
{
    public IEnumerable<string> Errors { get; } = Enumerable.Empty<string>();
    public Guid EventId { get; }

    public EventValidationException(Guid eventId)
        : base($"Событие с ID '{eventId}' не прошло валидацию.")
    {
        EventId = eventId;
    }
    
    public EventValidationException(Guid eventId, string message) : base(message)
    {
         EventId = eventId;
    }
    
    public EventValidationException(Guid eventId, IEnumerable<string> errors) 
        : base(string.Join("; ", errors))
    {
        EventId = eventId;
        Errors = errors;
    }
}