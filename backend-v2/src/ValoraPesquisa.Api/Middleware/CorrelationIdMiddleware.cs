namespace ValoraPesquisa.Api.Middleware;
public sealed class CorrelationIdMiddleware(RequestDelegate next){ public async Task Invoke(HttpContext ctx){ var id=ctx.Request.Headers.TryGetValue("X-Correlation-Id",out var v)&&!string.IsNullOrWhiteSpace(v)?v.ToString():Guid.NewGuid().ToString("n"); ctx.Items["CorrelationId"]=id; ctx.Response.Headers["X-Correlation-Id"]=id; await next(ctx);} }
