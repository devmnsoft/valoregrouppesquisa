using Microsoft.AspNetCore.Http;
using Valora.Api.Middleware;
using Xunit;

namespace Valora.Tests;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task Adds_correlation_id_to_items_and_response_header()
    {
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext(); context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "abc";
        await middleware.InvokeAsync(context);
        Assert.Equal("abc", context.Items[CorrelationIdMiddleware.ItemName]);
        Assert.Equal("abc", context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
    }
}
