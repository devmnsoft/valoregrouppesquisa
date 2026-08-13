using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Valora.Api.Middleware;
using Valora.Application.Exceptions;
using Npgsql;
using Xunit;

namespace Valora.Tests;

[Trait("Category", "Unit")]
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

    [Fact]
    public async Task Undefined_column_is_a_schema_mismatch_not_database_unavailability()
    {
        var exception = new PostgresException("column missing", "ERROR", "ERROR", "42703");
        var context = await Invoke(exception);
        var body = await ReadBody(context);
        Assert.Equal(500, context.Response.StatusCode);
        Assert.Contains("DATABASE_SCHEMA_MISMATCH", body);
        Assert.DoesNotContain("Banco de dados indisponível", body);
        Assert.DoesNotContain("column missing", body);
    }

    [Fact]
    public async Task Npgsql_connectivity_failure_is_service_unavailable()
    {
        var context = await Invoke(new NpgsqlException("connection failed"));
        var body = await ReadBody(context);
        Assert.Equal(503, context.Response.StatusCode);
        Assert.Contains("DATABASE_UNAVAILABLE", body);
        Assert.DoesNotContain("connection failed", body);
    }

    private static async Task<DefaultHttpContext> Invoke(Exception exception)
    {
        var middleware = new ErrorHandlingMiddleware(_ => throw exception, NullLogger<ErrorHandlingMiddleware>.Instance, new TestHostEnvironment());
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(context);
        return context;
    }

    private static async Task<string> ReadBody(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        return await new StreamReader(context.Response.Body).ReadToEndAsync();
    }
}


public sealed class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Production;
    public string ApplicationName { get; set; } = "Tests";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
}
