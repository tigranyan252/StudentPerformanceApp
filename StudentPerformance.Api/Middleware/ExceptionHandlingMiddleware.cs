using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using StudentPerformance.Api.Exceptions; // Make sure to add this using directive

namespace StudentPerformance.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred.");
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/problem+json";
            var statusCode = HttpStatusCode.InternalServerError; // Default to 500 Internal Server Error
            var title = "An unexpected error occurred.";
            var detail = "An internal server error occurred.";

            switch (exception)
            {
                case NotFoundException notFoundEx:
                    statusCode = HttpStatusCode.NotFound; // 404
                    title = "Resource Not Found";
                    detail = notFoundEx.Message;
                    break;
                case BadRequestException badRequestEx:
                    statusCode = HttpStatusCode.BadRequest; // 400
                    title = "Bad Request";
                    detail = badRequestEx.Message;
                    break;
                case ForbiddenException forbiddenEx:
                    statusCode = HttpStatusCode.Forbidden; // 403
                    title = "Forbidden";
                    detail = forbiddenEx.Message;
                    break;
                case ConflictException conflictEx:
                    statusCode = HttpStatusCode.Conflict; // 409
                    title = "Conflict";
                    detail = conflictEx.Message;
                    break;
                case ArgumentException argEx: // For general argument validation errors not caught by ModelState
                    statusCode = HttpStatusCode.BadRequest; // 400
                    title = "Invalid Argument";
                    detail = argEx.Message;
                    break;
                case InvalidOperationException invalidOpEx: // For business logic errors that prevent an operation
                    statusCode = HttpStatusCode.Conflict; // 409 (could also be 400 or 500 depending on context)
                    title = "Operation Conflict";
                    detail = invalidOpEx.Message;
                    break;
                // Add more specific exception types if needed
                default:
                    // For generic exceptions, we return a generic error message to avoid leaking sensitive information.
                    break;
            }

            context.Response.StatusCode = (int)statusCode;

            var problemDetails = new
            {
                type = $"https://httpstatuses.com/{(int)statusCode}", // Optional: link to status code description
                title = title,
                status = (int)statusCode,
                detail = detail,
                instance = context.Request.Path
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
        }
    }
}