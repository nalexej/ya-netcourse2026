using System.Text.Json;
using EventMgtApi.UsersService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace EventMgtApi.UsersService.Web.Middleware;

/// <summary>
/// Middleware для глобальной обработки исключений.
/// Перехватывает <see cref="ValidationException"/> и необработанные исключения,
/// возвращая стандартизированный ответ в формате <see cref="ProblemDetails"/>.
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Инициализирует новый экземпляр middleware для глобальной обработки исключений.
    /// </summary>
    /// <param name="next">Следующий делегат в конвейере обработки HTTP-запроса.</param>
    /// <param name="logger">Сервис логирования, используемый для записи информации о необработанных исключениях.</param>
    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Выполняет логику middleware: перехватывает исключения и возвращает ошибки в стандартизированном виде.
    /// </summary>
    /// <param name="context">Контекст HTTP-запроса.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Асинхронно обрабатывает необработанное исключение, записывает его в лог и формирует соответствующий ответ.
    /// Если ответ уже начат, обработка прерывается во избежание исключения при записи в тело ответа.
    /// </summary>
    /// <param name="httpContext">Контекст HTTP-запроса, в котором произошло исключение.</param>
    /// <param name="exception">Исключение, требующее обработки.</param>
    /// <remarks>
    /// Метод использует pattern matching для определения типа исключения и делегирует дальнейшую обработку 
    /// соответствующему обработчику: <see cref="HandleValidationExceptionAsync"/>, 
    /// или <see cref="HandleInternalServerErrorAsync"/>.
    /// </remarks>
    private async Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
    {
        if (httpContext.Response.HasStarted)
            return;

        await (exception switch
        {
            ValidationException ex => HandleValidationExceptionAsync(httpContext, ex),
            InvalidCredentialsException ex => HandleInvalidCredentialsExceptionAsync(httpContext, ex),
            _ => HandleInternalServerErrorAsync(httpContext)
        });
    }

    /// <summary>
    /// Обрабатывает <see cref="ValidationException"/>, формируя ответ с деталями валидации.
    /// </summary>
    /// <param name="context">Контекст HTTP-запроса.</param>
    /// <param name="ex">Исключение с деталями валидации.</param>
    private async Task HandleValidationExceptionAsync(HttpContext context, ValidationException ex)
    {
        // Логируем детали валидации
        // Используем только Errors, так как ModelState удален
        var errorMessages = ex.Errors
            .Where(kv => kv.Value != null && kv.Value.Any())
            .SelectMany(kv => kv.Value!.Select(v => $"{kv.Key}: {v}"))
            .ToList();

        _logger.LogError(
            "Ошибка валидации. Method={Method}, Path={Path}, Errors={Errors}, RequestId={RequestId}",
            context.Request.Method,
            context.Request.Path,
            string.Join("; ", errorMessages),
            context.Request.Headers["x-request-id"].ToString());

        var details = new ProblemDetails
        {
            Title = "Ошибка валидации",
            Status = 400,
            Detail = "Обнаружены ошибки валидации входных данных.",
            Instance = context.Request.Path,
        };

        // Формируем словарь ошибок для ProblemDetails
        var errors = ex.Errors
            .Where(kv => kv.Value != null)
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value.Select(e => e.ToString()).ToArray()
            );

        details.Extensions["errors"] = errors;

        await WriteProblemDetailsAsync(context, details);
    }

    /// <summary>
    /// Обрабатывает <see cref="InvalidCredentialsException"/>, возвращая 401 Unauthorized.
    /// </summary>
    private async Task HandleInvalidCredentialsExceptionAsync(HttpContext context, InvalidCredentialsException ex)
    {
        _logger.LogWarning(
            "Доступ запрещен. Method={Method}, Path={Path}, Message={Message}, RequestId={RequestId}",
            context.Request.Method,
            context.Request.Path,
            ex.Message,
            context.Request.Headers["x-request-id"].ToString());

        var details = new ProblemDetails
        {
            Title = "Доступ запрещен",
            Status = 401, // Возвращаем 401 Unauthorized
            Detail = ex.Message,
            Instance = context.Request.Path
        };

        await WriteProblemDetailsAsync(context, details);
    }

    /// <summary>
    /// Обрабатывает необработанные исключения, возвращая 500.
    /// </summary>
    /// <param name="context">Контекст HTTP-запроса.</param>
    private async Task HandleInternalServerErrorAsync(HttpContext context)
    {
        _logger.LogCritical(
            "Внутренняя ошибка сервера. Method={Method}, Path={Path}, RequestId={RequestId}, TraceId={TraceId}",
            context.Request.Method,
            context.Request.Path,
            context.Request.Headers["x-request-id"].ToString(), // закладка на будущее
            context.TraceIdentifier);

        var details = new ProblemDetails
        {
            Title = "Внутренняя ошибка сервера",
            Status = 500,
            Detail = "Произошла ошибка при обработке запроса.",
            Instance = context.Request.Path
        };

        context.Response.StatusCode = 500;
        await WriteProblemDetailsAsync(context, details);
    }

    /// <summary>
    /// Сериализует объект <see cref="ProblemDetails"/> в JSON и отправляет в ответ.
    /// </summary>
    /// <param name="context">Контекст HTTP-запроса.</param>
    /// <param name="details">Данные об ошибке.</param>
    private static async Task WriteProblemDetailsAsync(HttpContext context, ProblemDetails details)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = details.Status ?? 500;

        var json = JsonSerializer.Serialize(details, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(json);
    }
}