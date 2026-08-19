using EventMgtApi.EventsService.Application.Persistence;
using EventMgtApi.EventsService.Infrastructure.Persistence;
using EventMgtApi.EventsService.Infrastructure.Repositories;
using EventMgtApi.EventsService.Infrastructure.ServiceInteractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventMgtApi.EventsService.Infrastructure.DependencyInjection;
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Регистрация репозиториев
        services.AddScoped<IEventRepository, EventRepository>();
        //services.AddScoped<IBookingRepository, BookingRepository>();
        //services.AddScoped<IUserRepository, UserRepository>();

        // Регистрация DbContext
        services.AddDbContext<EventDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Регистрация фонового сервиса
        //services.AddHostedService<BookingProcessingBackgroundService>();
        services.AddHostedService<KafkaConsumerBackgroundService>();
        services.AddHostedService<KafkaTopicInitializer>();

        // Регистрация хеширования паролей
        //services.AddScoped<IPasswordHasher, PasswordHasher>();

        // Регистрация JWT-параметров и сервиса
        //var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        //services.Configure<JwtOptions>(options => jwtSection.Bind(options));
        //services.AddScoped<IJwtTokenService, JwtTokenService>();

        return services;
    }
}
