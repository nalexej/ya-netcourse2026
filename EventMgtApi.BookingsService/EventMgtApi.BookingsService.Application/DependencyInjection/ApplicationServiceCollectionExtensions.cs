using EventMgtApi.Application.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using EventMgtApi.BookingsService.Application.Services;
using EventMgtApi.BookingsService.Domain.Options;

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
            //services.Configure<SeedOptions>(options => configuration.GetSection("SeedOptions").Bind(options));

            return services;
        }
    }
}
