using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Valora.Api.Middleware;
using Valora.Application.Exceptions;
using Xunit;

namespace Valora.Tests;

public sealed class ErrorHandlingMiddlewareTests
{
    [Fact]
    public async Task Unhandled_exception_returns_standard_json_without_stack_in_production()
    {
        var env = new TestHostEnvironment { EnvironmentName = "Production" };
        var middleware = new ErrorHandlingMiddleware(_ => throw new Exception("boom"), NullLogger<ErrorHandlingMiddleware>.Instance, env);
        var context = new DefaultHttpContext(); context.Response.Body = new MemoryStream(); context.Items[CorrelationIdMiddleware.ItemName] = "corr-1";
        await middleware.InvokeAsync(context);
        context.Response.Body.Position = 0; var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Equal(500, context.Response.StatusCode); Assert.Contains("\"ok\":false", body); Assert.Contains("corr-1", body); Assert.DoesNotContain("boom", body); Assert.DoesNotContain("StackTrace", body);
    }

    [Theory]
    [InlineData(typeof(ValidationAppException), 400)]
    [InlineData(typeof(UnauthorizedAccessException), 401)]
    [InlineData(typeof(NotFoundAppException), 404)]
    [InlineData(typeof(BusinessRuleAppException), 422)]
    public async Task Maps_app_exceptions(Type type, int expected)
    {
        var env = new TestHostEnvironment { EnvironmentName = "Production" };
        var ex = (Exception)Activator.CreateInstance(type, "message")!;
        var middleware = new ErrorHandlingMiddleware(_ => throw ex, NullLogger<ErrorHandlingMiddleware>.Instance, env);
        var context = new DefaultHttpContext(); context.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(context);
        Assert.Equal(expected, context.Response.StatusCode);
    }
}


public sealed class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Production;
    public string ApplicationName { get; set; } = "Tests";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
}
