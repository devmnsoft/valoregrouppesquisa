using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Valora.Web.Models;
using Valora.Web.Services.Bff;

namespace Valora.Web.Controllers;

[ApiController]
public sealed class WebHealthController : ControllerBase
{
    private readonly ILogger<WebHealthController> _logger;
    private readonly ApiOptions _api;
    private readonly WebAppOptions _web;
    private readonly IWebHostEnvironment _environment;
    private readonly IBffApiClient _apiClient;

    public WebHealthController(ILogger<WebHealthController> logger, IOptions<ApiOptions> api, IOptions<WebAppOptions> web, IWebHostEnvironment environment, IBffApiClient apiClient)
    {
        _logger = logger;
        _api = api.Value;
        _web = web.Value;
        _environment = environment;
        _apiClient = apiClient;
    }

    [HttpGet("/health/web/api")]
    public async Task<IActionResult> Api(CancellationToken cancellationToken)
    {
        var correlationId = CorrelationId();
        try
        {
            await _apiClient.CheckHealthAsync(cancellationToken);
            return Ok(new { status = "ok", dependency = "Valora.Api", correlationId });
        }
        catch (BffApiUnavailableException exception)
        {
            _logger.LogWarning(exception, "Valora API dependency health failed. CorrelationId={CorrelationId} ApiBaseUrl={ApiBaseUrl}", correlationId, exception.BaseUrl);
            return StatusCode(503, new { status = "unavailable", dependency = "Valora.Api", correlationId });
        }
    }

    [HttpGet("/bff/system-health")]
    public async Task<IActionResult> SystemHealth(CancellationToken cancellationToken)
    {
        var correlationId = CorrelationId();
        var checks = new[] { "/health", "/health/database", "/health/migration", "/health/email", "/health/storage", "/health/version", "/health/config" };
        var results = new List<object>();

        foreach (var path in checks)
        {
            var checkName = path.Split('/').LastOrDefault() is { Length: > 0 } value ? value : "api";
            try
            {
                var payload = await _apiClient.GetHealthAsync(path, correlationId, cancellationToken);
                results.Add(new { name = checkName, status = "healthy", payload });
            }
            catch (BffApiException exception)
            {
                results.Add(new { name = checkName, status = "critical", message = "A dependência não respondeu como esperado.", correlationId = exception.CorrelationId ?? correlationId });
            }
            catch (BffApiUnavailableException)
            {
                results.Add(new { name = checkName, status = "critical", message = "A API está indisponível neste momento.", correlationId });
            }
        }

        var apiAvailable = results.Any(result => result.GetType().GetProperty("status")?.GetValue(result)?.ToString() == "healthy");
        return Ok(new
        {
            code = apiAvailable ? "SYSTEM_HEALTH_AVAILABLE" : "SYSTEM_HEALTH_DEGRADED",
            message = apiAvailable ? "Verificacao operacional concluida." : "Uma ou mais dependencias requerem atencao.",
            status = apiAvailable ? "attention" : "critical",
            correlationId,
            environment = _environment.EnvironmentName,
            version = _web.Version,
            web = "healthy",
            checks = results
        });
    }

    [HttpGet("/health/web")]
    public IActionResult Index()
    {
        try
        {
            return Ok(new { status = "ok", service = "Valora.Web", environment = _environment.EnvironmentName, version = _web.Version, correlationId = CorrelationId() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha no health check do Valora.Web");
            return StatusCode(503, new { status = "unavailable", service = "Valora.Web", correlationId = CorrelationId() });
        }
    }

    [HttpGet("/health/web/version")]
    public IActionResult Version()
    {
        try
        {
            return Ok(new { service = "Valora.Web", appName = _web.Name, version = _web.Version, environment = _environment.EnvironmentName, correlationId = CorrelationId() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao consultar versão do Valora.Web");
            return StatusCode(503, new { status = "unavailable", service = "Valora.Web", correlationId = CorrelationId() });
        }
    }

    [HttpGet("/health/web/config")]
    public IActionResult Config()
    {
        try
        {
            return Ok(new
            {
                service = "Valora.Web",
                environment = _environment.EnvironmentName,
                version = _web.Version,
                apiBaseUrl = _environment.IsDevelopment() ? _api.BaseUrl : "configured",
                publicUrl = _environment.IsDevelopment() ? _web.PublicUrl : "configured",
                timeoutMs = _api.TimeoutMs,
                correlationId = CorrelationId()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao consultar configuração segura do Valora.Web");
            return StatusCode(503, new { status = "unavailable", service = "Valora.Web", correlationId = CorrelationId() });
        }
    }

    private string CorrelationId() => Request.Headers.TryGetValue("X-Correlation-Id", out var id) && !string.IsNullOrWhiteSpace(id) ? id.ToString() : HttpContext.TraceIdentifier;
}
