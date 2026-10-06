using Serilog.Context;

namespace PayApi.Api.Middleware;

public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var id = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(id)) id = Guid.NewGuid().ToString("N");
        if (id.Length > 64) id = id[..64];

        context.Items[HeaderName] = id;
        context.Response.Headers[HeaderName] = id;

        using (LogContext.PushProperty("CorrelationId", id))
        {
            await _next(context);
        }
    }
}
