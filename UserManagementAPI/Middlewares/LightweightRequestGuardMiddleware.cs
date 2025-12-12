using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace UserManagementAPI.Middlewares;

public sealed class LightweightRequestGuardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LightweightRequestGuardMiddleware> _logger;

    // Defaults are safe for JSON APIs; override in appsettings if needed.
    private readonly HashSet<string> _allowedContentTypes;
    private readonly long _maxJsonBytes;

    public LightweightRequestGuardMiddleware(
        RequestDelegate next,
        ILogger<LightweightRequestGuardMiddleware> logger,
        IConfiguration config)
    {
        _next = next;
        _logger = logger;

        // Configurable via: "Security:AllowedContentTypes" and "Security:MaxJsonRequestBodyBytes"
        _allowedContentTypes = config
            .GetSection("Security:AllowedContentTypes").Get<string[]>()?
            .Select(ct => ct.ToLowerInvariant()).ToHashSet()
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "application/json",
                "application/*+json"
            };

        _maxJsonBytes = config.GetValue<long?>("Security:MaxJsonRequestBodyBytes") ?? 1_048_576L; // 1 MB default
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only guard methods that typically carry bodies
        if (HttpMethods.IsPost(context.Request.Method) ||
            HttpMethods.IsPut(context.Request.Method) ||
            HttpMethods.IsPatch(context.Request.Method))
        {
            // 1) Content-Type allow-list (no body buffering; cheap header check)
            var contentType = context.Request.ContentType;
            if (string.IsNullOrWhiteSpace(contentType) ||
                !_allowedContentTypes.Any(allowed => contentType.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("415 Unsupported Media Type. Content-Type: {ContentType}", contentType ?? "<none>");
                context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Title = "Unsupported media type.",
                    Detail = $"Allowed content types: {string.Join(", ", _allowedContentTypes)}",
                    Status = StatusCodes.Status415UnsupportedMediaType
                });
                return;
            }

            // 2) Request size cap: enforce via server feature without reading the body
            var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (feature is not null && !feature.IsReadOnly)
            {
                feature.MaxRequestBodySize = _maxJsonBytes; // must be set before body is read
            }

            // Fast-fail if Content-Length declares an oversize payload
            if (context.Request.ContentLength.HasValue && context.Request.ContentLength.Value > _maxJsonBytes)
            {
                _logger.LogWarning("413 Payload Too Large. Declared Content-Length: {Len} bytes", context.Request.ContentLength);
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Title = "Payload too large.",
                    Detail = $"Max allowed request body is {_maxJsonBytes} bytes.",
                    Status = StatusCodes.Status413PayloadTooLarge
                });
                return;
            }
        }

        await _next(context);
    }
}
