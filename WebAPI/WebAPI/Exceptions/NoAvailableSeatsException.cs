// Exceptions/NoAvailableSeatsException.cs

public class NoAvailableSeatsException : Exception
{
    public Guid EventId { get; }

    public NoAvailableSeatsException(Guid eventId)
        : base("No available seats for this event")
    {
        EventId = eventId;
    }
}