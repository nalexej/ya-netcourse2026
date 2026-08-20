using EventMgtApi.Application.Abstractions.Services;
using EventMgtApi.BookingsService.Application.Services;
using EventMgtApi.BookingsService.Domain.Options;
using EventMgtApi.Contracts.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventMgtApi.BookingsService.Application.DependencyInjection
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            // Регистрация сервисов уровня приложения (Use Cases / Services)
            services.AddScoped<IBookingService, BookingService>();

            services.Configure<BookingOptions>(options => configuration.GetSection("BookingOptions").Bind(options));
            services.Configure<KafkaOptions>(options => configuration.GetSection("Kafka").Bind(options));

            return services;
        }
    }
}
