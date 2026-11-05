namespace Risk.Api.Security;

public static class SecurityHeaders
{
    // For pages served by the API itself. SignalR (live viewing) needs ws/wss.
    public const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self' data:; " +
        "connect-src 'self' ws: wss:; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, bool includeContentSecurityPolicy)
    {
        return app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            if (includeContentSecurityPolicy)
            {
                headers["Content-Security-Policy"] = ContentSecurityPolicy;
            }

            await next();
        });
    }
}