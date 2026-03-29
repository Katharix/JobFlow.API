using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace JobFlow.Infrastructure.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly RequestDelegate _next;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Request was canceled by the client.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Response already started; cannot write error body.");
            return;
        }

        context.Response.Clear();
        context.Response.ContentType = "application/json";

        var apiError = new ApiError { Message = "An unexpected error occurred.", Code = "GENERAL_ERROR" };

        if (exception is TimeoutException)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            apiError = new ApiError
            {
                Message = "A required upstream service timed out. Please retry shortly.",
                Code = "UPSTREAM_TIMEOUT"
            };
        }
        else if (exception is HttpRequestException httpEx
                 && (httpEx.StatusCode == HttpStatusCode.GatewayTimeout
                     || httpEx.StatusCode == HttpStatusCode.BadGateway
                     || httpEx.StatusCode == HttpStatusCode.ServiceUnavailable))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            apiError = new ApiError
            {
                Message = "A required upstream service is currently unavailable. Please retry shortly.",
                Code = "UPSTREAM_UNAVAILABLE"
            };
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }

        await context.Response.WriteAsJsonAsync(apiError);
    }
}

public class ApiError
{
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}