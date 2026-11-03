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
           
            await Task.Delay(ProcessingDelayMs, stoppingToken);

            using var scope = _scopeFactory.CreateScope();
            
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            await bookingService.ProcessBookingAsync(bookingId, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Обработка брони {BookingId} отменена", bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Неожиданная ошибка при обработке брони {BookingId}", bookingId);
        }
    }
}