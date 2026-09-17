public class BookingService : IBookingService
{
    private readonly List<Booking> _bookings = new();
    private readonly ILogger<BookingService> _logger;
    private readonly IEventService _eventService;

    public BookingService(ILogger<BookingService> logger, IEventService eventService)
    {
        _logger = logger;
        _eventService = eventService;
    } 
    public async Task<Booking> CreateBookingAsync(Guid eventId)
    {

        if (eventId == Guid.Empty)
        throw new ArgumentException("EventId не может быть пустым", nameof(eventId));
        
        var eventEntity = _eventService.GetByIdEvent(eventId);
        if (eventEntity == null)
            throw new EventNotFoundException(eventId);

        var bookingEntity = Booking.Create(eventId);

        _bookings.Add(bookingEntity);

        _logger.LogInformation(
            "Создано бронирование: {BookingId} для события {EventId}",
            bookingEntity.Id,
            bookingEntity.EventId);
        
        return await Task.FromResult(bookingEntity);

    }
    public async Task<Booking> GetBookingByIdAsync(Guid id)
    {
        var bookingEntity = _bookings.FirstOrDefault(b => b.Id == id);
        
        if (bookingEntity == null)
            throw new BookingNotFoundException(id);
        
        return await Task.FromResult(bookingEntity);

    }
    public async Task<List<Booking>> GetBookingsByEventIdAsync(Guid eventId)
    {
        var bookingEntity = _bookings.Where(b => b.EventId == eventId ).OrderByDescending(b => b.CreatedAt).ToList();
        return await Task.FromResult(bookingEntity);
    }
    public async Task ConfirmBookingAsync(Guid id)
    {
         var booking = await GetBookingByIdAsync(id);
        booking.Confirm();
        
        _logger.LogInformation(
            "Подтверждено бронирование: {BookingId} для события {EventId}",
            booking.Id,
            booking.EventId);
    }
    public async Task RejectBookingAsync(Guid id)
    {
        var booking = await GetBookingByIdAsync(id);
        booking.Reject();
        
        _logger.LogInformation(
            "Отклонено бронирование: {BookingId} для события {EventId}",
            booking.Id,
            booking.EventId);
    }
    public async Task<List<Booking>> GetAllBookingsAsync()
    {
        var bookings = _bookings
            .OrderByDescending(b => b.CreatedAt)
            .ToList();
        
        return await Task.FromResult(bookings);
    }
    public async Task<List<Booking>> GetBookingsByStatusAsync(BookingStatus status)
    {
         var bookings = _bookings
            .Where(b => b.Status == status)
            .OrderByDescending(b => b.CreatedAt)
            .ToList();
        
        return await Task.FromResult(bookings);
    }
}