using EventMgtApi.EventsService.Application.Caching;
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

        // Регистрация Redis-опций
        services.Configure<RedisOptions>(
            config => configuration.GetSection(RedisOptions.SectionName).Bind(config));

        // Регистрация TTL-опций для кэша событий
        services.Configure<EventCacheOptions>(
            config => configuration.GetSection(EventCacheOptions.SectionName).Bind(config));

        // Регистрация Redis-клиента
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<RedisOptions>>().Value;

            var configurationOptions = new ConfigurationOptions
            {
                EndPoints = { opts.ConnectionString },
                ConnectTimeout = opts.ConnectTimeout,
                AbortOnConnectFail = opts.AbortOnConnectFail,
                ConnectRetry = opts.ConnectRetry,
            };

            return ConnectionMultiplexer.Connect(configurationOptions);
        });

        services.AddSingleton<ICacheClient, RedisCacheClient>();

        return services;
    }
}
