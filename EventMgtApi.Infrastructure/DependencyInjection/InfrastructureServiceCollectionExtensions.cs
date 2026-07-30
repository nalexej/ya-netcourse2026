using EventMgtApi.Application.Interfaces;
using EventMgtApi.Infrastructure.BackgroundServices;
using EventMgtApi.Infrastructure.Persistence;
using EventMgtApi.Infrastructure.Repositories;
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

            // Регистрация DbContext
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            // Регистрация фонового сервиса
            services.AddHostedService<BookingProcessingBackgroundService>();

            return services;
        }
    }
}
