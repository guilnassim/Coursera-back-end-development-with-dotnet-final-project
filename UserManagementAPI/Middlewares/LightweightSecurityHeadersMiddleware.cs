
namespace UserManagementAPI.Middlewares;

public sealed class LightweightSecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public LightweightSecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Don’t allow MIME-sniffing (helps prevent content-type confusion)
        headers.TryAdd("X-Content-Type-Options", "nosniff");

        // Keep referrer data minimal
        headers.TryAdd("Referrer-Policy", "no-referrer");

        // Lock cross-origin interactions (good defaults for APIs)
        headers.TryAdd("Cross-Origin-Opener-Policy", "same-origin");
        headers.TryAdd("Cross-Origin-Resource-Policy", "same-origin");

        // For pure APIs the value is mostly redundant,but harmless if responses are ever embedded.
        headers.TryAdd("X-Frame-Options", "DENY");        

        await _next(context);
    }
}
