using EventMgtApi.Application.Abstractions.Services;
using EventMgtApi.Application.DependencyInjection;
using EventMgtApi.Domain.Options;
using EventMgtApi.Infrastructure.BackgroundServices;
using EventMgtApi.Infrastructure.DependencyInjection;
using EventMgtApi.Infrastructure.Persistence;
using EventMgtApi.Web.Extensions;
using EventMgtApi.Web.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true; // валидируем сами
});

// Регистрация контроллеров
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ThrowValidationExceptionFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// JWT-аутентификация
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Bearer";
    options.DefaultChallengeScheme = "Bearer";
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
    };
});

// Регистрация слоев через расширения
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);



// Регистрация Swagger для документации API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Путь к XML-файлу с документацией
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Введите JWT токен в формате: Bearer {ваш_токен}",
        Name = "Authorization",       // Название заголовка HTTP
        In = ParameterLocation.Header, // Токен передаётся в заголовке запроса
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",              // Префикс, который Swagger будет подставлять автоматически
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document => new() { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });

    // Добавляем наш фильтр В КОНЕЦ списка
    options.OperationFilter<RemoveAuthForAnonymousOperations>();

});

var app = builder.Build();

// Создания объектов БД
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    
    var seedService = scope.ServiceProvider.GetRequiredService<ISeedService>();
    await seedService.SeedAsync();
}

// Настройка конвейера обработки запросов
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseGlobalExceptionHandling();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();


// Подключение маршрутизации контроллеров
app.MapControllers();

app.Run();