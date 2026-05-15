using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using EventMgtApi.Middleware;

namespace EventMgtApi.Extensions;

/// <summary>
/// Методы расширения для <see cref="IApplicationBuilder"/>.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Добавляет middleware для глобальной обработки исключений в конвейер HTTP-запросов.
    /// Middleware перехватывает необработанные исключения и возвращает стандартизированный ответ <see cref="ProblemDetails"/>.
    /// </summary>
    /// <param name="builder">Построитель конвейера HTTP-запросов.</param>
    /// <returns>Экземпляр <see cref="IApplicationBuilder"/> для цепочки вызовов.</returns>
    public static IApplicationBuilder UseGlobalExceptionHandling(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}