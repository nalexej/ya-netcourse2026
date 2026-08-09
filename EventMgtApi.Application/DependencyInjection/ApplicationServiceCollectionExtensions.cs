using EventMgtApi.Application.Abstractions.Services;
using EventMgtApi.Application.Services;
using EventMgtApi.Application.Users;
using EventMgtApi.Domain.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace EventMgtApi.Application.DependencyInjection
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            // Регистрация сервисов уровня приложения (Use Cases / Services)
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<IUserService, UserService>();

            services.Configure<BookingOptions>(options => configuration.GetSection("BookingSettings").Bind(options));

            return services;
        }
    }
}
