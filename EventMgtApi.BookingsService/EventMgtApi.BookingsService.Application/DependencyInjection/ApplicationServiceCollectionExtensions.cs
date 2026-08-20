using EventMgtApi.Application.Abstractions.Services;
using EventMgtApi.BookingsService.Application.ServiceInteraction;
using EventMgtApi.BookingsService.Application.Services;
using EventMgtApi.BookingsService.Domain.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventMgtApi.BookingsService.Application.DependencyInjection
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            // Регистрация сервисов уровня приложения (Use Cases / Services)
            //services.AddScoped<IEventService, EventService>();
            services.AddScoped<IBookingService, BookingService>();
            //services.AddScoped<IUserService, UserService>();
            //services.AddScoped<ISeedService, SeedService>();

            services.Configure<BookingOptions>(options => configuration.GetSection("BookingOptions").Bind(options));
            services.Configure<KafkaOptions>(options => configuration.GetSection("KafkaOptions").Bind(options));

            return services;
        }
    }
}
