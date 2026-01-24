using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stripe;
using Twilio.Exceptions;

namespace JobFlow.Infrastructure.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly IHostEnvironment _env;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly RequestDelegate _next;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var error = new ApiError();
        int statusCode;

        switch (exception)
        {
            case SqlException sqlEx:
                statusCode = StatusCodes.Status500InternalServerError;
                error.Message = "A database error occurred.";
                error.Code = "SQL_ERROR";
                break;

            case StripeException stripeEx:
                statusCode = (int)stripeEx.HttpStatusCode;
                error.Message = stripeEx.Message;
                error.Code = "STRIPE_ERROR";
                break;

            case HttpRequestException httpEx:
                statusCode = StatusCodes.Status503ServiceUnavailable;
                error.Message = "A service call failed.";
                error.Code = "HTTP_ERROR";
                break;
            case TwilioException twilioEx:
                statusCode = StatusCodes.Status500InternalServerError;
                error.Message = twilioEx.Message;
                error.Code = "TWILIO_ERROR";
                break;
            default:
                statusCode = StatusCodes.Status500InternalServerError;
                error.Message = "An unexpected error occurred.";
                error.Code = "GENERAL_ERROR";
                break;
        }

        if (_env.IsDevelopment()) error.Details = exception.ToString();

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(error);
    }
}

public class ApiError
{
    public string Message { get; set; }
    public string Code { get; set; }
    public string Details { get; set; }
}