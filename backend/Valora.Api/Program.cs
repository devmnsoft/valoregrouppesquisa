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

var configurationValidation = app.Services.GetRequiredService<Valora.Api.Operations.IConfigurationValidationService>().Validate();
if (app.Environment.IsProduction() && configurationValidation.Issues.Any(issue => issue.IsBlocking))
{
    var blockingIssueCodes = string.Join(", ", configurationValidation.Issues
        .Where(issue => issue.IsBlocking)
        .Select(issue => $"{issue.Category}/{issue.Code}"));
    app.Logger.LogCritical("Inicialização bloqueada por configuração de produção inválida. Issues: {IssueCodes}", blockingIssueCodes);
    throw new InvalidOperationException("Configuração insegura para produção. Consulte os registros de inicialização e o painel de Saúde do Sistema.");
}

if (app.Environment.IsDevelopment() && builder.Configuration.GetValue("Database:ValidateSchema", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<SchemaContractValidator>().ValidateAsync();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();
if (app.Environment.IsProduction())
{
    app.UseHsts();
    if (builder.Configuration.GetValue("Security:RequireHttps", true)) app.UseHttpsRedirection();
}
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});
app.UseSwagger();
app.UseSwaggerUI();
app.UseSerilogRequestLogging();
app.UseCors("ValoraWebCors");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<MaintenanceModeMiddleware>();

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
