using EventMgtApi.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventMgtApi.Application.DependencyInjection
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Регистрация сервисов уровня приложения (Use Cases / Services)
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IBookingService, BookingService>();

            return services;
        }
    }
}
