public class BookingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BookingBackgroundService> _logger;

    public BookingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<BookingBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookingBackgroundService запущен.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Создаём скоуп для получения сервисов
                using var scope = _serviceProvider.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                // Получаем все Pending брони
                var pendingBookings = await bookingService.GetBookingsByStatusAsync(BookingStatus.Pending);

                foreach (var booking in pendingBookings)
                {
                    // Имитация внешнего вызова (задержка 2 секунды)
                    await Task.Delay(2000, stoppingToken);

                    // Переводим в Confirmed (пока всегда подтверждаем)
                    await bookingService.ConfirmBookingAsync(booking.Id);

                    _logger.LogInformation(
                        "Бронирование {BookingId} обработано. Статус: Confirmed, ProcessedAt: {ProcessedAt}",
                        booking.Id,
                        DateTime.UtcNow);
                }

                // Пауза между циклами опроса (3 секунды)
                await Task.Delay(3000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в фоновом сервисе");
                await Task.Delay(5000, stoppingToken); 
            }
        }

        _logger.LogInformation("BookingBackgroundService остановлен.");
    }
}