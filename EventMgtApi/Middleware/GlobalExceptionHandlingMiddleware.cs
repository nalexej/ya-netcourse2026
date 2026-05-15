using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using EventMgtApi.Exceptions;

namespace EventMgtApi.Middleware;

/// <summary>
/// Middleware для глобальной обработки исключений.
/// Перехватывает <see cref="ValidationException"/>, <see cref="NotFoundException"/> и необработанные исключения,
/// возвращая стандартизированный ответ в формате <see cref="ProblemDetails"/>.
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Инициализирует новый экземпляр middleware.
    /// </summary>
    /// <param name="next">Следующий делегат в конвейере HTTP-запросов.</param>
    public GlobalExceptionHandlingMiddleware(RequestDelegate next) => _next = next;

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
        catch (ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (NotFoundException ex)
        {
            await HandleNotFoundExceptionAsync(context, ex);
        }
        catch (Exception)
        {
            await HandleInternalServerErrorAsync(context);
        }
    }

    /// <summary>
    /// Обрабатывает <see cref="ValidationException"/>, формируя ответ с деталями валидации.
    /// </summary>
    /// <param name="context">Контекст HTTP-запроса.</param>
    /// <param name="ex">Исключение с <see cref="ModelStateDictionary"/>.</param>
    private static async Task HandleValidationExceptionAsync(HttpContext context, ValidationException ex)
    {
        var details = new ProblemDetails
        {
            Title = "Ошибка валидации",
            Status = 400,
            Detail = "Обнаружены ошибки валидации входных данных.",
            Instance = context.Request.Path,
        };

        // Добавляем errors в Extensions → они попадут в JSON
        var errors = ex.ModelState
            .Where(kv => kv.Value!.Errors.Count > 0)
            .ToDictionary(
                kv => kv.Key,
                kv => (object?)kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
            );

        details.Extensions["errors"] = errors;

        await WriteProblemDetailsAsync(context, details);
    }

    /// <summary>
    /// Обрабатывает <see cref="NotFoundException"/>, возвращая 404.
    /// </summary>
    /// <param name="context">Контекст HTTP-запроса.</param>
    /// <param name="ex">Исключение с сообщением об отсутствующем ресурсе.</param>
    private static async Task HandleNotFoundExceptionAsync(HttpContext context, NotFoundException ex)
    {
        var details = new ProblemDetails
        {
            Title = "Ресурс не найден",
            Status = 404,
            Detail = ex.Message,
            Instance = context.Request.Path
        };

        await WriteProblemDetailsAsync(context, details);
    }

    /// <summary>
    /// Обрабатывает необработанные исключения, возвращая 500.
    /// </summary>
    /// <param name="context">Контекст HTTP-запроса.</param>
    private static async Task HandleInternalServerErrorAsync(HttpContext context)
    {
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