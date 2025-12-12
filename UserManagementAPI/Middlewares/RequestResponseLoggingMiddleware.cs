namespace UserManagementAPI.Middlewares;

public sealed class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue("X-Correlation-ID", out var values)
            ? values.ToString()
            : context.TraceIdentifier;

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            context.Response.Headers["X-Correlation-ID"] = correlationId;

            var method = context.Request.Method;
            var path = context.Request.Path;

            _logger.LogInformation("Request: {Method} {Path}", method, path);

            await _next(context);

            _logger.LogInformation("Response: {Method} {Path} → {StatusCode}", method, path, context.Response.StatusCode);
        }
    }
}
