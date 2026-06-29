using Serilog;
using Valora.Api.Configuration;
using Valora.Api.Middleware;
using Valora.Application.DependencyInjection;
using Valora.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseSerilogRequestLogging();
app.UseCors("ValoraWebCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Json(new
{
    ok = true,
    service = "Valora.Api",
    message = "Valora API operacional.",
    swagger = "/swagger",
    health = "/health"
}));

app.MapControllers();

app.Run();

public partial class Program { }
