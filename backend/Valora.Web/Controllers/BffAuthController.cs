using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Services.Bff;

namespace Valora.Web.Controllers;

[ApiController]
[AutoValidateAntiforgeryToken]
[Route("bff/auth")]
public sealed class BffAuthController(BffAuthenticationService authentication, IBffApiClient api,
    ILogger<BffAuthController> logger, IWebHostEnvironment environment) : ControllerBase {
    [AllowAnonymous, HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] object request, CancellationToken cancellationToken) {
        try {
            var rememberMe = request is JsonElement json
                && json.TryGetProperty("rememberMe", out var remember)
                && remember.ValueKind is JsonValueKind.True;
            return Ok(await authentication.SignInAsync(HttpContext, "/api/v1/auth/login", request, cancellationToken, rememberMe));
        }
        catch (BffApiUnavailableException exception) {
            var correlationId = CorrelationId();
            logger.LogError(exception, "Valora API unavailable during BFF login. CorrelationId={CorrelationId} ApiBaseUrl={ApiBaseUrl}",
                correlationId, exception.BaseUrl);
            var message = environment.IsDevelopment()
                ? $"A API Valora não está disponível em {exception.BaseUrl}. Inicie a Valora.Api ou ajuste Api:BaseUrl."
                : "O serviço de autenticação está temporariamente indisponível. Tente novamente em instantes.";
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "API_UNAVAILABLE", message, correlationId });
        }
        catch (BffApiException exception) {
            var correlationId = exception.CorrelationId ?? CorrelationId();
            logger.LogWarning(exception, "Authentication API rejected BFF login. Status={Status} Code={Code} CorrelationId={CorrelationId}", (int)exception.StatusCode, exception.Code, correlationId);
            return StatusCode((int)exception.StatusCode, new {
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
    public async Task<IActionResult> Forgot([FromBody] object request, CancellationToken cancellationToken) {
        await api.PostAsync("/api/v1/auth/forgot-password", request, null, cancellationToken);
        return Accepted(new { ok = true });
    }

    [AllowAnonymous, HttpPost("reset-password")]
    public async Task<IActionResult> Reset([FromBody] object request, CancellationToken cancellationToken) {
        await api.PostAsync("/api/v1/auth/reset-password", request, null, cancellationToken);
        return Ok(new { ok = true });
    }

    [Authorize, HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken) {
        await authentication.SignOutAsync(HttpContext, cancellationToken);
        return NoContent();
    }

    [Authorize, HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken) {
        var session = await authentication.RefreshAsync(HttpContext, cancellationToken);
        return session is null ? Unauthorized() : Ok(session);
    }

    [Authorize, HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken) {
        var session = await authentication.GetAsync(HttpContext, cancellationToken);
        return session is null ? Unauthorized() : Ok(session.SafeSession);
    }

    [Authorize, HttpGet("sessions")]
    public async Task<IActionResult> Sessions(CancellationToken cancellationToken) {
        var session = await authentication.GetAsync(HttpContext, cancellationToken);
        return session is null ? Unauthorized() : Ok(new[] { session.SafeSession });
    }

    [Authorize(Roles = "admin_valora"), HttpPost("select-organization")]
    public async Task<IActionResult> SelectOrganization([FromBody] SelectOrganizationRequest request,
        CancellationToken cancellationToken) {
        if (request.OrganizationId == Guid.Empty)
            return BadRequest(new { code = "ORGANIZATION_ID_REQUIRED", message = "Selecione uma organização válida." });

        var correlationId = CorrelationId();
        using var validation = await authentication.SendAuthorizedAsync(HttpContext, HttpMethod.Post,
            $"/api/v1/saas/customers/{request.OrganizationId}/select-context",
            new { reason = request.Reason }, correlationId, cancellationToken);
        if (validation is null) return Unauthorized(new { code = "SESSION_EXPIRED", message = "Sua sessão expirou." });
        if (!validation.IsSuccessStatusCode) {
            var payload = await validation.Content.ReadAsStringAsync(cancellationToken);
            return new ContentResult {
                StatusCode = (int)validation.StatusCode,
                ContentType = validation.Content.Headers.ContentType?.ToString() ?? "application/problem+json",
                Content = payload
            };
        }

        var responsePayload = await validation.Content.ReadAsStringAsync(cancellationToken);
        Guid selectedOrganizationId = request.OrganizationId;
        using var document = JsonDocument.Parse(responsePayload);
        if (document.RootElement.TryGetProperty("organizationId", out var organizationElement)
            && organizationElement.ValueKind == JsonValueKind.String
            && Guid.TryParse(organizationElement.GetString(), out var parsedOrganizationId)) {
            selectedOrganizationId = parsedOrganizationId;
        }

        var session = await authentication.SelectOrganizationAsync(HttpContext, selectedOrganizationId, cancellationToken);
        return session is null ? Forbid() : Ok(session);
    }

    [Authorize(Roles = "admin_valora"), HttpDelete("selected-organization")]
    public async Task<IActionResult> ClearSelectedOrganization(CancellationToken cancellationToken) {
        var session = await authentication.SelectOrganizationAsync(HttpContext, null, cancellationToken);
        return session is null ? Forbid() : Ok(session);
    }

    [Authorize(Roles = "admin_valora"), HttpGet("organizations")]
    public async Task<IActionResult> Organizations(CancellationToken cancellationToken) {
        var correlationId = CorrelationId();
        using var response = await authentication.SendAuthorizedAsync(HttpContext, HttpMethod.Get,
            "/api/v1/saas/customers?page=1&pageSize=100&status=active", null, correlationId, cancellationToken);
        if (response is null) return Unauthorized(new { code = "SESSION_EXPIRED", message = "Sua sessão expirou." });
        if (!response.IsSuccessStatusCode) {
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Listagem de clientes SaaS indisponível para o seletor de organizações. Status={Status} CorrelationId={CorrelationId}", (int)response.StatusCode, correlationId);
            return new ContentResult {
                StatusCode = (int)response.StatusCode,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/problem+json",
                Content = payload
            };
        }

        var organizations = new List<object>();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (document.RootElement.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array) {
            foreach (var item in items.EnumerateArray()) {
                if (!item.TryGetProperty("organizationId", out var organizationElement) || organizationElement.ValueKind != JsonValueKind.String) continue;
                if (!Guid.TryParse(organizationElement.GetString(), out var organizationId)) continue;
                var name = item.TryGetProperty("tradeName", out var tradeName) && tradeName.ValueKind == JsonValueKind.String
                    ? tradeName.GetString()?.Trim() ?? "" : "";
                organizations.Add(new { organizationId, name = string.IsNullOrWhiteSpace(name) ? organizationId.ToString() : name });
            }
        }
        return Ok(organizations);
    }
}

public sealed record SelectOrganizationRequest(Guid OrganizationId, string? Reason);
