// Путь: Middleware/ErrorHandlingMiddleware.cs

using Microsoft.AspNetCore.Http; // Для RequestDelegate, HttpContext
using Microsoft.Extensions.Logging; // Для ILogger
using System; // Для Exception
using System.Net; // Для HttpStatusCode
using System.Text.Json; // Для JsonSerializer

namespace StudentPerformance.Api.Middleware // !!! Убедитесь, что namespace правильный !!!
{
    // Middleware для централизованной обработки ошибок
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next; // Следующий делегат в конвейере запросов
        private readonly ILogger<ErrorHandlingMiddleware> _logger; // Логгер для записи ошибок

        // Конструктор Middleware
        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        // Метод Invoke или InvokeAsync (для асинхронных операций) обрабатывает запрос
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Передаем запрос следующему Middleware в конвейере
                await _next(context);
            }
            catch (Exception ex)
            {
                // Если произошло исключение, перехватываем его
                _logger.LogError(ex, "An unhandled exception occurred."); // Логируем ошибку

                // Обрабатываем исключение и формируем ответ
                await HandleExceptionAsync(context, ex);
            }
        }

        // Вспомогательный метод для формирования HTTP ответа на основе исключения
        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Устанавливаем стандартный код состояния HTTP
            context.Response.ContentType = "application/json"; // Устанавливаем тип содержимого как JSON

            // Определяем статус код и сообщение в зависимости от типа исключения
            // Вы можете добавить обработку других специфических исключений здесь
            var statusCode = HttpStatusCode.InternalServerError; // По умолчанию 500 Internal Server Error
            var message = "An unexpected error occurred.";
            var details = exception.Message; // В продакшене лучше не раскрывать детали внутренних ошибок клиенту

            // Пример обработки специфических исключений:
            if (exception is UnauthorizedAccessException) // Если сервис выбросил UnauthorizedAccessException
            {
                statusCode = HttpStatusCode.Forbidden; // 403 Forbidden
                message = "You do not have permission to perform this action.";
                details = null; // Не раскрываем детали для безопасности
            }
            else if (exception is ArgumentException argEx) // Например, ошибки валидации или неверные аргументы
            {
                statusCode = HttpStatusCode.BadRequest; // 400 Bad Request
                message = "Invalid request parameters.";
                details = argEx.Message; // Можно включить сообщение об ошибке валидации
            }
            // Добавьте другие else if для других специфических исключений, которые вы выбрасываете в сервисах


            context.Response.StatusCode = (int)statusCode; // Устанавливаем код состояния ответа

            // Формируем тело ответа в формате ProblemDetails (или другом стандартизованном формате)
            // ProblemDetails - стандартизованный формат для HTTP API ошибок (RFC 7807)
            var responseBody = new
            {
                status = (int)statusCode,
                title = message,
                detail = details,
                instance = context.Request.Path // Путь запроса, который вызвал ошибку
                // Можно добавить другие поля из ProblemDetails (type, ...)
            };

            // Сериализуем объект ответа в JSON и записываем в тело ответа
            var result = JsonSerializer.Serialize(responseBody);
            return context.Response.WriteAsync(result); // Асинхронно записываем ответ
        }
    }
}