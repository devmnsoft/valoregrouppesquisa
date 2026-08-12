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
