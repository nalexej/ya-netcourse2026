using EventMgtApi.UsersService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EventMgtApi.UsersService.Web.Filters;

/// <summary>
/// Фильтр действий, который проверяет состояние модели (<see cref="ModelStateDictionary"/>).
/// Если есть ошибки валидации, выбрасывает <see cref="ValidationException"/>.
/// </summary>
public class ThrowValidationExceptionFilter : IActionFilter, IOrderedFilter
{
    /// <summary>
    /// Определяет порядок фильтра. Выполняется как можно раньше (после привязки модели).
    /// </summary>
    public int Order => int.MinValue;

    /// <summary>
    /// Выполняется перед вызовом действия контроллера.
    /// Проверяет, валидна ли модель. Если нет — выбрасывает исключение.
    /// </summary>
    /// <param name="context">Контекст выполнения действия.</param>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            // Создаем словарь с явным указанием типов ключа и значения (ICollection<string>)
            var errors = new Dictionary<string, ICollection<string>>();

            foreach (var kvp in context.ModelState)
            {
                // Проверяем, что Value не null и есть ошибки
                if (kvp.Value?.Errors.Count > 0)
                {
                    // Преобразуем Errors в List (который реализует ICollection)
                    errors.Add(kvp.Key, kvp.Value.Errors.Select(e => e.ErrorMessage).ToList());
                }
            }

            throw new ValidationException(errors);
        }
    }

    /// <summary>
    /// Выполняется после действия. Ничего не делает — обработка ошибок в middleware.
    /// </summary>
    /// <param name="context">Контекст завершённого действия.</param>
    public void OnActionExecuted(ActionExecutedContext context) { }
}