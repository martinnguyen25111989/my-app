using System.Net;
using System.Text.Json;
using ExpenseManager.Application.Common.Exceptions;
using FluentValidation;

namespace ExpenseManager.API.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
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
            ValidationException ve => (HttpStatusCode.BadRequest, "Dữ liệu không hợp lệ.",
                (object?)ve.Errors.Select(e => new { field = e.PropertyName, error = e.ErrorMessage })),
            BadRequestException e => (HttpStatusCode.BadRequest, e.Message, null),
            UnauthorizedException e => (HttpStatusCode.Unauthorized, e.Message, null),
            ForbiddenException e => (HttpStatusCode.Forbidden, e.Message, null),
            NotFoundException e => (HttpStatusCode.NotFound, e.Message, null),
            ConflictException e => (HttpStatusCode.Conflict, e.Message, null),
            _ => (HttpStatusCode.InternalServerError, "Đã xảy ra lỗi hệ thống.", null)
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            logger.LogError(exception, "Lỗi không xác định: {Message}", exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            statusCode = (int)statusCode,
            message,
            errors
        }, JsonOptions));
    }
}
