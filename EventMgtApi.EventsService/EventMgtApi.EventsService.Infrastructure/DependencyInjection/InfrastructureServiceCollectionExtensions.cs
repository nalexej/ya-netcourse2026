using EventMgtApi.Contracts.Options;
using EventMgtApi.Contracts.ServiceInteraction;
using EventMgtApi.EventsService.Application.Persistence;
using EventMgtApi.EventsService.Infrastructure.Persistence;
using EventMgtApi.EventsService.Infrastructure.Persistence.Repositories;
using EventMgtApi.EventsService.Infrastructure.Repositories;
using EventMgtApi.EventsService.Infrastructure.ServiceInteraction;
using EventMgtApi.EventsService.Infrastructure.ServiceInteractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EventMgtApi.EventsService.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Регистрация репозиториев
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IProcessedBookingRepository, ProcessedBookingRepository>();

        // Регистрация DbContext
        services.AddDbContext<EventDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Регистрация сервисов
        services.AddHostedService<KafkaTopicInitializer>();
        services.AddHostedService<EventServiceMessagingConsumer>();
        services.AddSingleton<IEventPublisher, EventServiceMessagingPublisher>();

        // Регистрация Redis-клиента
        var redisOptions = new RedisOptions();
        configuration.GetSection(RedisOptions.SectionName).Bind(redisOptions);
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisOptions.ConnectionString));
        services.AddSingleton<ICacheClient, RedisCacheClient>();

        return services;
    }
}
