using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Services.Bff;
using System.Text.Json;

namespace Valora.Web.Controllers;

[ApiController]
[AutoValidateAntiforgeryToken]
[Route("bff/auth")]
public sealed class BffAuthController(BffAuthenticationService authentication, IBffApiClient api,
    ILogger<BffAuthController> logger, IWebHostEnvironment environment) : ControllerBase
{
    [AllowAnonymous, HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] object request, CancellationToken cancellationToken)
    {
        try
        {
            var rememberMe = request is JsonElement json
                && json.TryGetProperty("rememberMe", out var remember)
                && remember.ValueKind is JsonValueKind.True;
            return Ok(await authentication.SignInAsync(HttpContext, "/api/v1/auth/login", request, cancellationToken, rememberMe));
        }
        catch (BffApiUnavailableException exception)
        {
            var correlationId = CorrelationId();
            logger.LogError(exception, "Valora API unavailable during BFF login. CorrelationId={CorrelationId} ApiBaseUrl={ApiBaseUrl}",
                correlationId, exception.BaseUrl);
            var message = environment.IsDevelopment()
                ? $"A API Valora não está disponível em {exception.BaseUrl}. Inicie a Valora.Api ou ajuste Api:BaseUrl."
                : "O serviço de autenticação está temporariamente indisponível. Tente novamente em instantes.";
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "API_UNAVAILABLE", message, correlationId });
        }
        catch (BffApiException exception)
        {
            var correlationId = exception.CorrelationId ?? CorrelationId();
            logger.LogWarning(exception, "Authentication API rejected BFF login. Status={Status} Code={Code} CorrelationId={CorrelationId}", (int)exception.StatusCode, exception.Code, correlationId);
            return StatusCode((int)exception.StatusCode, new
            {
                status = (int)exception.StatusCode,
                code = exception.Code,
                message = exception.Message,
                correlationId
            });
        }
    }

    private string CorrelationId() => Request.Headers.TryGetValue("X-Correlation-Id", out var value)
        && !string.IsNullOrWhiteSpace(value) ? value.ToString() : HttpContext.TraceIdentifier;

    [AllowAnonymous, HttpPost("register-company")]
    public async Task<IActionResult> Register([FromBody] object request, CancellationToken cancellationToken) =>
        Ok(await authentication.SignInAsync(HttpContext, "/api/v1/auth/register-company", request, cancellationToken));

    [AllowAnonymous, HttpPost("forgot-password")]
    public async Task<IActionResult> Forgot([FromBody] object request, CancellationToken cancellationToken)
    {
        await api.PostAsync("/api/v1/auth/forgot-password", request, null, cancellationToken);
        return Accepted(new { ok = true });
    }

    [AllowAnonymous, HttpPost("reset-password")]
    public async Task<IActionResult> Reset([FromBody] object request, CancellationToken cancellationToken)
    {
        await api.PostAsync("/api/v1/auth/reset-password", request, null, cancellationToken);
        return Ok(new { ok = true });
    }

    [Authorize, HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await authentication.SignOutAsync(HttpContext, cancellationToken);
        return NoContent();
    }

    [Authorize, HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var session = await authentication.RefreshAsync(HttpContext, cancellationToken);
        return session is null ? Unauthorized() : Ok(session);
    }

    [Authorize, HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var session = await authentication.GetAsync(HttpContext, cancellationToken);
        return session is null ? Unauthorized() : Ok(session.SafeSession);
    }

    [Authorize, HttpGet("sessions")]
    public async Task<IActionResult> Sessions(CancellationToken cancellationToken)
    {
        var session = await authentication.GetAsync(HttpContext, cancellationToken);
        return session is null ? Unauthorized() : Ok(new[] { session.SafeSession });
    }
}
