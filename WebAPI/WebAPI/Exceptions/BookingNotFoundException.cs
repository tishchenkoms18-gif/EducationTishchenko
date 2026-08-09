
public class BookingNotFoundException : Exception
{
    public BookingNotFoundException() { }
    public BookingNotFoundException(string message) : base(message) { }
    public BookingNotFoundException(string message, System.Exception inner) : base(message, inner) { }
    

    public Guid BookingId { get; }

    public BookingNotFoundException(Guid bookingId)
        : base($"Событие с ID '{bookingId}' не найдено.")
    {
        BookingId = bookingId;
    }

    public BookingNotFoundException(Guid bookingId, string message)
        : base(message)
    {
        BookingId = bookingId;
    }
}