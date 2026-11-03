using System;
using Microsoft.Extensions.DependencyInjection;


namespace WebAPI.DependencyInjection;

public static class ApplicationExtensions
{
 public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Здесь регистрируем сервисы бизнес-логики
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        return services;
    }
}
