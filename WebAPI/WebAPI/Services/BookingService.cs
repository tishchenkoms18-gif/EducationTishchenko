// Services/BookingService.cs
using Microsoft.EntityFrameworkCore;
using WebAPI.DataAccess;

public class BookingService : IBookingService
{
    private readonly AppDbContext _context;
    private readonly ILogger<BookingService> _logger;

    // ✅ static SemaphoreSlim — для синхронизации между экземплярами
    private static readonly SemaphoreSlim _bookingSemaphore = new(1, 1);

    public BookingService(AppDbContext context, ILogger<BookingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Booking> CreateBookingAsync(Guid eventId)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("EventId не может быть пустым", nameof(eventId));

        await _bookingSemaphore.WaitAsync();   // 🔒 Захватываем асинхронно
        try
        {
            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == eventId)
                ?? throw new EventNotFoundException(eventId);

            if (!eventEntity.TryReserveSeats(1))
                throw new NoAvailableSeatsException(eventId);

            var booking = Booking.Create(eventId);
            _context.Bookings.Add(booking);

            // ✅ Один SaveChangesAsync сохраняет и бронь, и уменьшение AvailableSeats
            await _context.SaveChangesAsync();

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
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
            throw new BookingNotFoundException(id);

        return booking;
    }

    public async Task<List<Booking>> GetBookingsByEventIdAsync(Guid eventId)
    {
        return await _context.Bookings
            .Where(b => b.EventId == eventId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task ConfirmBookingAsync(Guid id)
    {
        var booking = await GetBookingByIdAsync(id);
        booking.Confirm();
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Подтверждено бронирование: {BookingId} для события {EventId}",
            booking.Id, booking.EventId);
    }

    public async Task RejectBookingAsync(Guid id)
    {
        var booking = await GetBookingByIdAsync(id);
        booking.Reject();

        // ✅ Возвращаем место в пул события
        var eventEntity = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == booking.EventId);

        if (eventEntity != null)
        {
            eventEntity.ReleaseSeats(1);
        }
        else
        {
            _logger.LogWarning(
                "Не удалось вернуть место: событие {EventId} не найдено", booking.EventId);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Отклонено бронирование: {BookingId} для события {EventId}",
            booking.Id, booking.EventId);
    }

    public async Task<List<Booking>> GetAllBookingsAsync()
    {
        return await _context.Bookings
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Booking>> GetBookingsByStatusAsync(BookingStatus status)
    {
        return await _context.Bookings
            .Where(b => b.Status == status)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }
}