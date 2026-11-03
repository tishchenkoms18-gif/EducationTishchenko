using Microsoft.Extensions.Logging;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<BookingService> _logger;

    // ✅ static SemaphoreSlim — для синхронизации между экземплярами
    private static readonly SemaphoreSlim _bookingSemaphore = new(1, 1);

    public BookingService(
        IBookingRepository bookingRepository, 
        IEventRepository eventRepository,
        ILogger<BookingService> logger)
    {
        _bookingRepository = bookingRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<Booking> CreateBookingAsync(Guid eventId)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("EventId не может быть пустым", nameof(eventId));

        await _bookingSemaphore.WaitAsync();   
        try
        {
            var eventEntity = await _eventRepository.GetByIdAsync(eventId)
                ?? throw new EventNotFoundException(eventId);

            if (!eventEntity.TryReserveSeats(1))
                throw new NoAvailableSeatsException(eventId);

            var booking = Booking.Create(eventId);
            
            await _bookingRepository.AddAsync(booking);
            
            
            await _eventRepository.UpdateAsync(eventEntity);

            await _bookingRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Создана бронь {BookingId} для события {EventId}. Осталось мест: {AvailableSeats}",
                booking.Id, eventId, eventEntity.AvailableSeats);

            return booking;
        }
        finally
        {
            _bookingSemaphore.Release();       // 🔓 Отпускаем в любом случае
        }
    }

    public async Task<Booking> GetBookingByIdAsync(Guid id)
    {
        var booking = await _bookingRepository.GetByIdAsync(id);

        if (booking == null)
            throw new BookingNotFoundException(id);

        return booking;
    }

    public async Task<List<Booking>> GetBookingsByEventIdAsync(Guid eventId)
    {
        return await _bookingRepository.GetByEventIdAsync(eventId);
    }

    public async Task ConfirmBookingAsync(Guid id)
    {
        var booking = await GetBookingByIdAsync(id);
        booking.Confirm();
        
        await _bookingRepository.UpdateAsync(booking);
        await _bookingRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Подтверждено бронирование: {BookingId} для события {EventId}",
            booking.Id, booking.EventId);
    }

    public async Task RejectBookingAsync(Guid id)
    {
        var booking = await GetBookingByIdAsync(id);
        booking.Reject();

        var eventEntity = await _eventRepository.GetByIdAsync(booking.EventId);

        if (eventEntity != null)
        {
            eventEntity.ReleaseSeats(1);
            await _eventRepository.UpdateAsync(eventEntity);
        }
        else
        {
            _logger.LogWarning(
                "Не удалось вернуть место: событие {EventId} не найдено", booking.EventId);
        }

        await _bookingRepository.UpdateAsync(booking);
        await _bookingRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Отклонено бронирование: {BookingId} для события {EventId}",
            booking.Id, booking.EventId);
    }

    public async Task<List<Booking>> GetAllBookingsAsync()
    {
        return await _bookingRepository.GetAllAsync();
    }

    public async Task<List<Booking>> GetBookingsByStatusAsync(BookingStatus status)
    {
        return await _bookingRepository.GetByStatusAsync(status);
    }
}