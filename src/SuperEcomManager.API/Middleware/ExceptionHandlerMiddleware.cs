using System.Net;
using System.Text.Json;
using SuperEcomManager.Application.Common.Exceptions;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.API.Middleware;

/// <summary>
/// Global exception handler middleware.
/// </summary>
public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

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

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, errors) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                "Validation failed",
                validationEx.Errors.SelectMany(e => e.Value).ToArray()
            ),
            NotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                notFoundEx.Message,
                Array.Empty<string>()
            ),
            UnauthorizedException => (
                HttpStatusCode.Unauthorized,
                "Authentication required",
                Array.Empty<string>()
            ),
            ForbiddenAccessException forbiddenEx => (
                HttpStatusCode.Forbidden,
                forbiddenEx.Message,
                Array.Empty<string>()
            ),
            FeatureDisabledException featureEx => (
                HttpStatusCode.PaymentRequired,
                featureEx.Message,
                Array.Empty<string>()
            ),
            TenantNotFoundException tenantEx => (
                HttpStatusCode.BadRequest,
                tenantEx.Message,
                Array.Empty<string>()
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred",
                Array.Empty<string>()
            )
        };

        // Log the exception
        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception: {Message}", exception.Message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.Fail(message, errors);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}
