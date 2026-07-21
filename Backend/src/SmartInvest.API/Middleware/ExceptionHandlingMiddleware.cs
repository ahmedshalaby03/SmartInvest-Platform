using System.Net;
using System.Text.Json;
using FluentValidation;
using SmartInvest.Application.Common.Exceptions;

namespace SmartInvest.API.Middleware;

public class ExceptionHandlingMiddleware
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        object response;

        switch (exception)
        {
            case NotFoundException notFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response = new { message = notFoundException.Message };
                break;

            case BusinessRuleException businessRuleException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new { message = businessRuleException.Message };
                break;

            case ValidationException validationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                var errors = validationException.Errors.Select(e => e.ErrorMessage);
                response = new { message = "بيانات غير صحيحة", errors };
                break;

            default:
                _logger.LogError(exception, "Unhandled exception occurred");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response = new { message = "حدث خطأ غير متوقع" };
                break;
        }

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}