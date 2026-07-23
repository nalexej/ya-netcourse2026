using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using EventMgtApi.Domain.Exceptions;

namespace EventMgtApi.Web.Filters;

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
            throw new ValidationException(context.ModelState);
        }
    }

    /// <summary>
    /// Выполняется после действия. Ничего не делает — обработка ошибок в middleware.
    /// </summary>
    /// <param name="context">Контекст завершённого действия.</param>
    public void OnActionExecuted(ActionExecutedContext context) { }
}