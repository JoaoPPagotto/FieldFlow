using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FieldFlow.Application.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FieldFlow.Api.Middleware;

/// <summary>
/// Converte qualquer exceção em <b>RFC 7807 Problem Details</b> (com extensões <c>code</c>,
/// <c>errors</c> e <c>traceId</c>) e grava um log estruturado único por falha.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            context.Request.EnableBuffering();
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        (int Status, string Code, string Title, object? Details) info = exception switch
        {
            AppException app              => (app.StatusCode, app.Code, app.Message, app.Details),
            FluentValidation.ValidationException v => (400, ErrorCodes.ValidationFailed, "Validation failed",
                                                 v.Errors.Select(e => new { e.PropertyName, Code = e.ErrorCode, Message = e.ErrorMessage })),
            UnauthorizedAccessException   => (401, ErrorCodes.Unauthorized, "Unauthorized access", null),
            KeyNotFoundException          => (404, ErrorCodes.ResourceNotFound, exception.Message, null),
            ArgumentException             => (400, ErrorCodes.InvalidArgument, exception.Message, null),
            DbUpdateConcurrencyException  => (409, ErrorCodes.Conflict, "The resource was modified by another request", null),
            DbUpdateException             => (500, ErrorCodes.DatabaseError, "A database error occurred", null),
            InvalidOperationException     => (400, ErrorCodes.InvalidOperation, exception.Message, null),
            _                             => (500, ErrorCodes.InternalError, "An internal server error occurred", null)
        };
        var (status, code, title, details) = info;

        await LogFailureAsync(context, exception, status, code);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = $"https://httpstatuses.io/{status}",
            Instance = context.Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        if (details is not null) problem.Extensions["errors"] = details;
        if (status >= 500 && _environment.IsDevelopment()) problem.Detail = exception.Message;

        if (context.Response.HasStarted) return;

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }

    /// <summary>Um único ponto de log por falha (no padrão original a cadeia era repetida em cada case).</summary>
    private async Task LogFailureAsync(HttpContext context, Exception exception, int status, string code)
    {
        var body = await GetRequestBodyAsync(context);
        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["user_id"] = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous",
            ["user_email"] = context.User.FindFirstValue(ClaimTypes.Email) ?? "anonymous",
            ["ip"] = context.Connection.RemoteIpAddress?.ToString(),
            ["endpoint"] = context.Request.Path.Value,
            ["operation"] = context.Request.Method,
            ["resource_id"] = context.Request.RouteValues.TryGetValue("key", out var k) ? k?.ToString() : null,
            ["error_code"] = code,
            ["status_code"] = status,
            ["request_body"] = body,
            ["result"] = "Failure"
        });

        if (status >= 500)
            _logger.LogError(exception, "Request failed with {StatusCode} {ErrorCode}", status, code);
        else
            _logger.LogWarning("Request failed with {StatusCode} {ErrorCode}: {Message}", status, code, exception.Message);
    }

    private static async Task<string?> GetRequestBodyAsync(HttpContext context)
    {
        if (context.Request.Method is not ("POST" or "PUT" or "PATCH")) return null;
        if (!(context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? false)) return null;
        if (!context.Request.Body.CanSeek) return null;

        context.Request.Body.Position = 0;
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        return string.IsNullOrWhiteSpace(body) ? null : body;
    }
}
