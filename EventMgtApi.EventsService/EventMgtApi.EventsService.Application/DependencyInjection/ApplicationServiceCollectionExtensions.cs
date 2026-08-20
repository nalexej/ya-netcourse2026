using EventMgtApi.Application.Abstractions.Services;
using EventMgtApi.Contracts.Options;
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
        services.Configure<KafkaOptions>(options => configuration.GetSection("Kafka").Bind(options));

        return services;
    }
}
