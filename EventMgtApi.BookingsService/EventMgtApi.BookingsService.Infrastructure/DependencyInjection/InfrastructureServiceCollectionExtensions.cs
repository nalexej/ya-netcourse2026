using EventMgtApi.BookingsService.Application.Persistence;
using EventMgtApi.BookingsService.Application.ServiceInteraction;
using EventMgtApi.BookingsService.Infrastructure.BackgroundServices;
using EventMgtApi.BookingsService.Infrastructure.Repositories;
using EventMgtApi.BookingsService.Infrastructure.ServiceInteraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventMgtApi.BookingsService.Infrastructure.DependencyInjection
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Регистрация репозиториев
            //services.AddScoped<IEventRepository, EventRepository>();
            services.AddScoped<IBookingRepository, BookingRepository>();
            //services.AddScoped<IUserRepository, UserRepository>();

            // Регистрация DbContext
            services.AddDbContext<BookingDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            // Регистрация фонового сервиса
            services.AddHostedService<BookingProcessingBackgroundService>();
            services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

            // Регистрация хеширования паролей
            //services.AddScoped<IPasswordHasher, PasswordHasher>();

            // Регистрация JWT-параметров и сервиса
            //var jwtSection = configuration.GetSection(JwtOptions.SectionName);
            //services.Configure<JwtOptions>(options => jwtSection.Bind(options));
            //services.AddScoped<IJwtTokenService, JwtTokenService>();

            return services;
        }
    }
}
