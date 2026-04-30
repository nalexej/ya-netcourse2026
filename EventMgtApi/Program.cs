using EventMgtApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Регистрация контроллеров
builder.Services.AddControllers();
builder.Services.AddSingleton<IEventService, EventService>();

var app = builder.Build();

app.UseHttpsRedirection();

// Подключение маршрутизации контроллеров
app.MapControllers();

app.Run();
