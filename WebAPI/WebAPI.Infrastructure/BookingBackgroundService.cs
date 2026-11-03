using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


public class BookingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingBackgroundService> _logger;

    private const int PollingIntervalMs = 3000;
    private const int ProcessingDelayMs = 2000;

    public BookingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingBackgroundService> logger)
    { 
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookingBackgroundService запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                List<Guid> pendingBookingIds;

                // 1. Получаем список Pending ID в отдельном scope
                using (var scope = _scopeFactory.CreateScope())
                {
                    var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                    
                    var pendingBookings = await bookingRepository.GetByStatusAsync(BookingStatus.Pending);
                    
                    // Передаём только ID, чтобы не тащить сущности между разными DbContext
                    pendingBookingIds = pendingBookings.Select(b => b.Id).ToList();
                }

                // 2. Обрабатываем каждую бронь параллельно, создавая свой scope
                if (pendingBookingIds.Any())
                {
                    var tasks = pendingBookingIds.Select(id => ProcessBookingAsync(id, stoppingToken));
                    await Task.WhenAll(tasks);
                }

                await Task.Delay(PollingIntervalMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в фоновом сервисе");
                await Task.Delay(PollingIntervalMs, stoppingToken);
            }
        }

        _logger.LogInformation("BookingBackgroundService остановлен");
    }

    /// <summary>
    /// Обработка одной брони: имитация внешнего вызова → подтверждение/отклонение
    /// </summary>
    private async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
    {
        try
        {
            // Имитация внешнего вызова
            await Task.Delay(ProcessingDelayMs, stoppingToken);

            //  Свой scope на каждую бронь
            using var scope = _scopeFactory.CreateScope();
            var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
            var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

            var booking = await bookingRepository.GetByIdAsync(bookingId);
            if (booking == null)
            {
                _logger.LogWarning("Бронь {BookingId} не найдена при обработке", bookingId);
                return;
            }

            // Проверяем, существует ли событие
            var eventEntity = await eventRepository.GetByIdAsync(booking.EventId);

            if (eventEntity == null)
            {
                // Событие удалено → отклоняем бронь
                booking.Reject();
                await bookingRepository.UpdateAsync(booking);
                await bookingRepository.SaveChangesAsync();
                
                _logger.LogWarning(
                    "Бронь {BookingId} отклонена: событие {EventId} не найдено",
                    booking.Id, booking.EventId);
                return;
            }

            // Всё ок → подтверждаем
            booking.Confirm();
            await bookingRepository.UpdateAsync(booking);
            await bookingRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Бронь {BookingId} подтверждена для события {EventId}",
                booking.Id, booking.EventId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Обработка брони {BookingId} отменена", bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Неожиданная ошибка при обработке брони {BookingId}", bookingId);

            //  Пытаемся отклонить бронь и вернуть место при непредвиденной ошибке
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

                var booking = await bookingRepository.GetByIdAsync(bookingId);
                if (booking != null && booking.Status == BookingStatus.Pending)
                {
                    booking.Reject();
                    
                    var eventEntity = await eventRepository.GetByIdAsync(booking.EventId);
                    if (eventEntity != null)
                    {
                        eventEntity.ReleaseSeats(1);
                        await eventRepository.UpdateAsync(eventEntity);
                    }

                    await bookingRepository.UpdateAsync(booking);
                    await bookingRepository.SaveChangesAsync();
                }
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "Ошибка при отклонении брони {BookingId}", bookingId);
            }
        }
    }
}