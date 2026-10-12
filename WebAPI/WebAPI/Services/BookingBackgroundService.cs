
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
                // 1. Получаем список Pending ID в отдельном scope
                List<Guid> pendingBookingIds;

                using (var scope = _scopeFactory.CreateScope())
                {
                    var bookingService = scope.ServiceProvider
                        .GetRequiredService<IBookingService>();

                    var pendingBookings = await bookingService
                        .GetBookingsByStatusAsync(BookingStatus.Pending);

                    pendingBookingIds = pendingBookings
                        .Select(b => b.Id)
                        .ToList();
                }

                // 2. Обрабатываем каждую бронь параллельно — свой scope на каждую
                if (pendingBookingIds.Any())
                {
                    var tasks = pendingBookingIds.Select(id =>
                        ProcessBookingAsync(id, stoppingToken));

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
            // Имитация внешнего вызова (до открытия scope, чтобы задержки шли параллельно)
            await Task.Delay(ProcessingDelayMs, stoppingToken);

            // ✅ Свой scope на каждую бронь
            using var scope = _scopeFactory.CreateScope();
            var bookingService = scope.ServiceProvider
                .GetRequiredService<IBookingService>();
            var eventService = scope.ServiceProvider
                .GetRequiredService<IEventService>();

            // Загружаем бронь в текущем scope
            var booking = await bookingService.GetBookingByIdAsync(bookingId);

            // Проверяем, существует ли ещё событие
            try
            {
                await eventService.GetByIdEventAsync(booking.EventId);
            }
            catch (EventNotFoundException)
            {
                // Событие удалено → отклоняем бронь (RejectBookingAsync вернёт место, если найдёт)
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
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Обработка брони {BookingId} отменена", bookingId);
        }
        catch (BookingNotFoundException ex)
        {
            _logger.LogWarning(
                "Бронь {BookingId} не найдена при обработке: {Message}",
                bookingId, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки брони {BookingId}", bookingId);

            // Пытаемся отклонить бронь в новом scope (старый мог быть повреждён)
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var bookingService = scope.ServiceProvider
                    .GetRequiredService<IBookingService>();

                await bookingService.RejectBookingAsync(bookingId);
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx,
                    "Ошибка при отклонении брони {BookingId}", bookingId);
            }
        }
    }
}