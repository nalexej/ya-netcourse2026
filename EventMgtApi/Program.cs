using EventMgtApi.Services;
using System.Reflection;
using EventMgtApi.Extensions;
using EventMgtApi.Filters;
using EventMgtApi.Repositories;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true; // валидируем сами
});

// Регистрация контроллеров
//builder.Services.AddControllers();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ThrowValidationExceptionFilter>();
});
builder.Services.AddSingleton<IEventRepository, InMemoryEventRepository>();
builder.Services.AddSingleton<IEventService, EventService>();

// Регистрация Swagger для документации API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Путь к XML-файлу с документацией
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Настройка конвейера обработки запросов
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseGlobalExceptionHandling();

app.UseHttpsRedirection();

// Подключение маршрутизации контроллеров
app.MapControllers();

app.Run();