using System.Text;
using Serilog.Context;

namespace Valora.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemName = "CorrelationId";
    public const int MaxLength = 100;

    public async Task InvokeAsync(HttpContext context)
    {
        var received = context.Request.Headers.TryGetValue(HeaderName, out var header) ? header.ToString() : null;
        var correlationId = IsValid(received) ? Sanitize(received!) : Guid.NewGuid().ToString("N");

        context.Items[ItemName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength) return false;
        return value.All(c => !char.IsControl(c) && c is not '\r' and not '\n');
    }

    public static string Sanitize(string value)
    {
        var builder = new StringBuilder(Math.Min(value.Length, MaxLength));
        foreach (var c in value.Take(MaxLength))
        {
            if (!char.IsControl(c) && c is not '\r' and not '\n') builder.Append(c);
        }
        return builder.Length == 0 ? Guid.NewGuid().ToString("N") : builder.ToString();
    }
}
