
public class BookingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BookingBackgroundService> _logger;

    // 🔒 Семафор для защиты записи в хранилище (нельзя lock с await)
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    private const int PollingIntervalMs = 3000;
    private const int ProcessingDelayMs = 2000;

    public BookingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<BookingBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
       _logger.LogInformation("BookingBackgroundService запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                List<Booking> pendingBookings;

                // ✅ Создаём scope, чтобы получить сервис
                using (var scope = _serviceProvider.CreateScope())
                {
                    var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                    pendingBookings = await bookingService.GetBookingsByStatusAsync(BookingStatus.Pending);
                }

                if (pendingBookings.Any())
                {
                    // ✅ Каждая бронь — свой scope
                    var tasks = pendingBookings.Select(b => ProcessBookingAsync(b, stoppingToken));
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

        _logger.LogInformation("BookingBackgroundService остановлен.");
    }

    /// <summary>
    /// Обработка одной брони: имитация внешнего вызова → подтверждение/отклонение
    /// </summary>
    private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(ProcessingDelayMs, stoppingToken);
            using var scope = _serviceProvider.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

            await _processingSemaphore.WaitAsync(stoppingToken); // 🔒
            try
            {
                // Проверяем, существует ли событие
                Event? eventEntity = null;
                try
                {
                    eventEntity = await eventService.GetByIdEventAsync(booking.EventId);
                }
                catch (EventNotFoundException)
                {
                    // Событие удалено → отклоняем бронь
                    await bookingService.RejectBookingAsync(booking.Id);
                    _logger.LogWarning(
                        "Бронь {BookingId} отклонена: событие {EventId} не найдено",
                        booking.Id, booking.EventId);
                    return;
                }

                // Всё ок → подтверждаем
                await bookingService.ConfirmBookingAsync(booking.Id);
                _logger.LogInformation(
                    "Бронь {BookingId} подтверждена для события {EventId}",
                    booking.Id, booking.EventId);
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Обработка брони {BookingId} отменена", booking.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Неожиданная ошибка при обработке брони {BookingId}", booking.Id);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                await bookingService.RejectBookingAsync(booking.Id);
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "Ошибка при отклонении брони {BookingId}", booking.Id);
            }
        }
    }
}