using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EventMgtApi.UsersService.Web.Middleware;


/// <summary>
/// Фабрика для создания экземпляра <see cref="JwtBearerEvents"/> с кастомной обработкой
/// ответов аутентификации и авторизации.
/// Возвращает стандартизированные ProblemDetails вместо пустых ответов (401/403)
/// при ошибках JWT-аутентификации.
/// </summary>
public class JwtBearerEventsFactory
{
    /// <summary>
    /// Создаёт настроенный экземпляр <see cref="JwtBearerEvents"/>.
    /// Настраивает обработчики OnChallenge и OnForbidden для возврата
    /// структурированной JSON-ошибки вместо пустого HTTP-ответа.
    /// </summary>
    /// <returns>Настроенный экземпляр <see cref="JwtBearerEvents"/>.</returns>
    public JwtBearerEvents Create()
    {
        return new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                // Говорим системе, что мы сами сформируем ответ
                context.HandleResponse();

                var title = "Unauthorized";
                var status = StatusCodes.Status401Unauthorized;
                var detail = "Требуется аутентификация.";

                // Если токен пытались передать, но проверка сорвалась с ошибкой
                if (context.AuthenticateFailure != null)
                {
                    detail = $"Недействительный токен: {context.AuthenticateFailure.Message}";
                }

                var details = new ProblemDetails
                {
                    Title = title,
                    Status = status,
                    Detail = detail,
                    Instance = context.Request.Path
                };

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = status;

                return context.Response.WriteAsync(JsonSerializer.Serialize(details, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
            },

            OnForbidden = async context =>
            {
                var details = new ProblemDetails
                {
                    Title = "Forbidden",
                    Status = StatusCodes.Status403Forbidden,
                    Detail = "Недостаточно прав для доступа к этому ресурсу.",
                    Instance = context.HttpContext.Request.Path
                };

                context.HttpContext.Response.ContentType = "application/json";
                context.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(details, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
            }
        };
    }
}