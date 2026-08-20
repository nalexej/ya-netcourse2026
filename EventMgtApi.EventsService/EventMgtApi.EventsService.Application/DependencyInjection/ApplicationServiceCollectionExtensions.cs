using EventMgtApi.Application.Abstractions.Services;
using EventMgtApi.EventsService.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventMgtApi.EventsService.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // Регистрация сервисов уровня приложения (Use Cases / Services)
        services.AddScoped<IEventService, EventService>();
        //services.AddScoped<IBookingService, BookingService>();
        //services.AddScoped<IUserService, UserService>();
        //services.AddScoped<ISeedService, SeedService>();

        //services.Configure<BookingOptions>(options => configuration.GetSection("BookingOptions").Bind(options));
        //services.Configure<SeedOptions>(options => configuration.GetSection("SeedOptions").Bind(options));

        return services;
    }
}
