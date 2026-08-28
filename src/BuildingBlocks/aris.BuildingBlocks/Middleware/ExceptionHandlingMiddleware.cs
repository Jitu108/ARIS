using aris.BuildingBlocks.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace aris.BuildingBlocks.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (AppException ex)
        {
            var correlationId = context.GetCorrelationId();
            _logger.LogWarning(ex, "Handled application exception. CorrelationId: {CorrelationId}", correlationId);
            await WriteProblemAsync(context, ex.Type, ex.Title, ex.StatusCode, ex.Message, correlationId);
        }
        catch (Exception ex)
        {
            var correlationId = context.GetCorrelationId();
            _logger.LogError(ex, "Unhandled exception. CorrelationId: {CorrelationId}", correlationId);
            await WriteProblemAsync(
                context,
                "https://aris.dev/problems/internal-server-error",
                "An unexpected error occurred.",
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred while processing the request.",
                correlationId);
        }
    }

    private static Task WriteProblemAsync(HttpContext context, string type, string title, int statusCode, string detail, string correlationId)
    {
        var problem = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = statusCode,
            Detail = detail,
        };
        problem.Extensions["traceId"] = correlationId;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(problem);
    }
}
