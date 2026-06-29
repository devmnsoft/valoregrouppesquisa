using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Valora.Web.Models;

namespace Valora.Web.Controllers;

[ApiController]
public sealed class WebHealthController : ControllerBase
{
    private readonly ILogger<WebHealthController> _logger;
    private readonly ApiOptions _api;
    private readonly WebAppOptions _web;
    private readonly IWebHostEnvironment _environment;

    public WebHealthController(ILogger<WebHealthController> logger, IOptions<ApiOptions> api, IOptions<WebAppOptions> web, IWebHostEnvironment environment)
    {
        _logger = logger;
        _api = api.Value;
        _web = web.Value;
        _environment = environment;
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
            return Ok(new { service = "Valora.Web", environment = _environment.EnvironmentName, version = _web.Version, apiBaseUrl = MaskUrl(_api.BaseUrl), publicUrl = _web.PublicUrl, timeoutMs = _api.TimeoutMs, correlationId = CorrelationId() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao consultar configuração segura do Valora.Web");
            return StatusCode(503, new { status = "unavailable", service = "Valora.Web", correlationId = CorrelationId() });
        }
    }

    private string CorrelationId() => Request.Headers.TryGetValue("X-Correlation-Id", out var id) && !string.IsNullOrWhiteSpace(id) ? id.ToString() : HttpContext.TraceIdentifier;
    private static string MaskUrl(string? url) => string.IsNullOrWhiteSpace(url) ? string.Empty : url.Replace("//", "//***@", StringComparison.Ordinal);
}
