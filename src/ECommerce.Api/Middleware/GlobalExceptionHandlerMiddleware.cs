using System.Text.Json;
using ECommerce.Application.Common;
using ECommerce.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Middleware;

/// <summary>
/// Catches unhandled exceptions and converts them into a consistent RFC 7807
/// Problem Details response. Detailed diagnostics are logged internally; clients
/// never receive stack traces or internal exception details.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
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
        var requestId = context.TraceIdentifier;

        var (statusCode, title, detail, errors) = exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                "One or more validation errors occurred.",
                (IReadOnlyDictionary<string, string[]>?)validationException.Errors),

            InvalidQuantityException or InsufficientInventoryException => (
                StatusCodes.Status400BadRequest,
                "Invalid request",
                exception.Message,
                null),

            BusinessRuleViolationException => (
                StatusCodes.Status409Conflict,
                "Business rule violation",
                exception.Message,
                null),

            UnauthorizedAccessException => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "You do not have permission to perform this action.",
                null),

            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource not found",
                exception.Message,
                null),

            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "An internal error occurred. Please try again later.",
                null)
        };

        // Log full details internally; never expose them to the client.
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception. RequestId={RequestId} Path={Path}", requestId, context.Request.Path);
        }
        else
        {
            _logger.LogWarning("Handled exception ({ExceptionType}) returned {StatusCode}. RequestId={RequestId} Path={Path} Message={Message}",
                exception.GetType().Name, statusCode, requestId, context.Request.Path, exception.Message);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.io/{statusCode}",
            Instance = context.Request.Path
        };
        problemDetails.Extensions["requestId"] = requestId;

        if (errors is { Count: > 0 })
        {
            problemDetails.Extensions["errors"] = errors;
        }

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

/// <summary>
/// Extension methods for registering the global exception handler middleware.
/// </summary>
public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
