using EventMgtApi.Application.Abstractions.Services;
using EventMgtApi.Contracts.Options;
using EventMgtApi.UsersService.Application.Abstractions.Persistence;
using EventMgtApi.UsersService.Application.Abstractions.Services;
using EventMgtApi.UsersService.Domain.Options;
using EventMgtApi.UsersService.Infrastructure.Persistence;
using EventMgtApi.UsersService.Infrastructure.Persistence.Repositories;
using EventMgtApi.UsersService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventMgtApi.UsersService.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Регистрация репозиториев
        //services.AddScoped<IEventRepository, EventRepository>();
        //services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISeedService, SeedService>();

        services.Configure<SeedOptions>(options => configuration.GetSection("SeedOptions").Bind(options));

        // Регистрация DbContext
        services.AddDbContext<UserDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Регистрация фонового сервиса
        //services.AddHostedService<BookingProcessingBackgroundService>();

        // Регистрация хеширования паролей
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        // Регистрация JWT-параметров и сервиса
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        services.Configure<JwtOptions>(options => jwtSection.Bind(options));
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        return services;
    }
}
