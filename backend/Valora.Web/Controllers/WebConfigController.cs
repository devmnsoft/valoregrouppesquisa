using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Valora.Web.Models;

namespace Valora.Web.Controllers;

[ApiController]
public sealed class WebConfigController : ControllerBase
{
    private readonly ILogger<WebConfigController> _logger;
    private readonly ApiOptions _api;
    private readonly WebAppOptions _web;
    private readonly IWebHostEnvironment _environment;

    public WebConfigController(ILogger<WebConfigController> logger, IOptions<ApiOptions> api, IOptions<WebAppOptions> web, IWebHostEnvironment environment)
    {
        _logger = logger;
        _api = api.Value;
        _web = web.Value;
        _environment = environment;
    }

    [HttpGet("/web-config.js")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Get()
    {
        try
        {
            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";
            Response.Headers["X-Content-Type-Options"] = "nosniff";

            var payload = new
            {
                APP_NAME = Safe(_web.Name, "Valora Pulse"),
                APP_VERSION = Safe(_web.Version, "1.0.0-web"),
                ENVIRONMENT = Safe(_web.Environment, _environment.EnvironmentName),
                API_BASE_URL = Safe(_api.BaseUrl, "http://localhost:5080"),
                API_TIMEOUT_MS = _api.TimeoutMs > 0 ? _api.TimeoutMs : 20000,
                PUBLIC_URL = Safe(_web.PublicUrl, "http://localhost:5088"),
                ENABLE_DEBUG_LOGS = _web.EnableDebugLogs && !_environment.IsProduction()
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { Encoder = JavaScriptEncoder.Default });
            return Content($"window.ValoraWebConfig = Object.freeze({json});", "application/javascript; charset=utf-8");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao gerar configuração dinâmica do Valora.Web");
            var fallback = "window.ValoraWebConfig = Object.freeze({APP_NAME:'Valora Pulse',APP_VERSION:'1.0.0-web',ENVIRONMENT:'Unavailable',API_BASE_URL:'',API_TIMEOUT_MS:20000,PUBLIC_URL:'',ENABLE_DEBUG_LOGS:false});";
            return Content(fallback, "application/javascript; charset=utf-8");
        }
    }

    private static string Safe(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
