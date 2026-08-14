using Serilog;
using Valora.Api.Configuration;
using Valora.Api;
using Valora.Api.Middleware;
using Valora.Application.DependencyInjection;
using Valora.Infrastructure.DependencyInjection;
using Valora.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddMemoryCache();
builder.Services.Configure<Valora.Api.Configuration.FreeSurveySecurityOptions>(builder.Configuration.GetSection("FreeSurveySecurity"));
builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.Configure<IntelligenceProcessingOptions>(builder.Configuration.GetSection("Valora:Processing"));
builder.Services.AddHostedService<IntelligenceProcessingWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment() && builder.Configuration.GetValue("Database:ValidateSchema", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<SchemaContractValidator>().ValidateAsync();
}

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
