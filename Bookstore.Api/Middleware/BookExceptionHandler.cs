using System.Text.Json;
using Bookstore.Api.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Middleware;

public sealed class BookExceptionHandler(
    ILogger<BookExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            BookValidationException validationException =>
                (StatusCodes.Status400BadRequest, "Bad Request", validationException.Message),
            BadHttpRequestException or JsonException =>
                (StatusCodes.Status400BadRequest, "Bad Request", "The request body contains invalid JSON."),
            _ => (0, string.Empty, string.Empty)
        };

        if (statusCode == 0)
            return false;

        logger.LogWarning(exception, "Rejected bookstore request with status {StatusCode}", statusCode);
        httpContext.Response.StatusCode = statusCode;

        var traceId = RequestTraceId.Get(httpContext);
        var problem = new ProblemDetails
        {
            Type = "about:blank",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = $"urn:trace:{traceId}"
        };
        problem.Extensions["traceId"] = traceId;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem
        });
    }
}

internal static class RequestTraceId
{
    internal const string ItemKey = "Bookstore.TraceId";

    internal static string Get(HttpContext httpContext) =>
        httpContext.Items.TryGetValue(ItemKey, out var value) && value is string traceId
            ? traceId
            : httpContext.TraceIdentifier;
}
