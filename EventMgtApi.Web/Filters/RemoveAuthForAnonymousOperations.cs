namespace EventMgtApi.Web.Filters
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.OpenApi;
    using Swashbuckle.AspNetCore.SwaggerGen;

    /// <summary>
    /// Фильтр операций Swagger, который скрывает значок авторизации (замок) 
    /// для эндпоинтов, помеченных атрибутом <see cref="AllowAnonymousAttribute"/>.
    /// </summary>
    public class RemoveAuthForAnonymousOperations : IOperationFilter
    {

        /// <summary>
        /// Применяет фильтр к заданной операции Swagger.
        /// </summary>
        /// <param name="operation">Текущая модель операции OpenAPI.</param>
        /// <param name="context">Контекст генератора операций, предоставляющий метаданные отражения метода и типа.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // 1. Проверяем наличие атрибута [AllowAnonymous] на самом методе
            var hasAllowAnonymousOnMethod = context.MethodInfo.GetCustomAttributes(true)
                .OfType<AllowAnonymousAttribute>()
                .Any();

            // 2. Проверяем наличие атрибута [AllowAnonymous] на контроллере (классе)
            bool hasAllowAnonymousOnClass = false;
            if (context.MethodInfo.DeclaringType != null)
            {
                hasAllowAnonymousOnClass = context.MethodInfo.DeclaringType
                    .GetCustomAttributes(true)
                    .OfType<AllowAnonymousAttribute>()
                    .Any();
            }

            // 3. Если атрибут найден хотя бы в одном месте — очищаем требования безопасности
            if (hasAllowAnonymousOnMethod || hasAllowAnonymousOnClass)
            {
                // Полностью удаляем секцию "security" из этого метода в swagger.json
                operation.Security = new List<OpenApiSecurityRequirement>();
            }
        }
    }
}
