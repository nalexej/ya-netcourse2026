using EventMgtApi.Application.Abstractions.Services;
using EventMgtApi.Application.Interfaces;
using EventMgtApi.Domain.Options;
using EventMgtApi.Infrastructure.BackgroundServices;
using EventMgtApi.Infrastructure.Persistence;
using EventMgtApi.Infrastructure.Repositories;
using EventMgtApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventMgtApi.Infrastructure.DependencyInjection
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Регистрация репозиториев
            services.AddScoped<IEventRepository, EventRepository>();
            services.AddScoped<IBookingRepository, BookingRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            // Регистрация DbContext
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            // Регистрация фонового сервиса
            services.AddHostedService<BookingProcessingBackgroundService>();

            // Регистрация хеширования паролей
            services.AddScoped<IPasswordHasher, PasswordHasher>();

            // Регистрация JWT-параметров и сервиса
            var jwtSection = configuration.GetSection(JwtOptions.SectionName);
            services.Configure<JwtOptions>(options => jwtSection.Bind(options));
            services.AddScoped<IJwtTokenService, JwtTokenService>();

            return services;
        }
    }
}
