using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Services.Bff;

namespace Valora.Web.Controllers;

/// <summary>Typed same-origin gateway for every dedicated intelligence workspace.</summary>
[Authorize, ApiController, AutoValidateAntiforgeryToken, Route("bff/intelligence")]
public sealed class BffIntelligenceController(IBffApiClient api, BffAuthenticationService authentication, ILogger<BffIntelligenceController> logger) : ControllerBase {
    [AcceptVerbs("GET", "POST", "PATCH", "DELETE")]
    [Route("{**resource}")]
    public async Task<IActionResult> Forward(string? resource, CancellationToken ct) {
        var session = await authentication.GetAsync(HttpContext, ct);
        if (session is null) return Unauthorized(new { code = "SESSION_EXPIRED", message = "Sua sessão expirou. Entre novamente.", correlationId = HttpContext.TraceIdentifier });

        object? body = null;
        if (Request.ContentLength > 0)
            body = await JsonSerializer.DeserializeAsync<JsonElement>(Request.Body, cancellationToken: ct);

        var correlationId = Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        HttpResponseMessage? response;
        try {
            response = await authentication.SendAuthorizedAsync(HttpContext, new HttpMethod(Request.Method),
                $"/api/v1/intelligence/{resource}{Request.QueryString}", body, correlationId, ct);
        }
        catch (BffApiUnavailableException exception) when (IsApiTimeout(exception, ct)) {
            logger.LogWarning(exception,
                "Timeout while forwarding Intelligence request to Valora.Api. Method={Method} Resource={Resource} CorrelationId={CorrelationId}",
                Request.Method, resource, correlationId);
            return StatusCode(StatusCodes.Status504GatewayTimeout, new {
                status = StatusCodes.Status504GatewayTimeout,
                code = "API_TIMEOUT",
                message = "A operação demorou mais que o esperado. Tente novamente.",
                correlationId
            });
        }
        catch (BffApiUnavailableException exception) when (!ct.IsCancellationRequested) {
            logger.LogWarning(exception,
                "Valora.Api unavailable while forwarding Intelligence request. Method={Method} Resource={Resource} CorrelationId={CorrelationId}",
                Request.Method, resource, correlationId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new {
                status = StatusCodes.Status503ServiceUnavailable,
                code = "API_UNAVAILABLE",
                message = "API temporariamente indisponível. Tente novamente em instantes.",
                correlationId
            });
        }
        if (response is null) return Unauthorized(new { code = "SESSION_EXPIRED", message = "Sua sessão expirou. Entre novamente.", correlationId });
        using (response) {
            var payload = await response.Content.ReadAsStringAsync(ct);
            Response.Headers["X-Correlation-Id"] = response.Headers.TryGetValues("X-Correlation-Id", out var values) ? values.First() : correlationId;
            return new ContentResult { StatusCode = (int)response.StatusCode, ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json", Content = payload };
        }
    }

    private static bool IsApiTimeout(BffApiUnavailableException exception, CancellationToken requestToken) =>
        !requestToken.IsCancellationRequested && exception.InnerException is TaskCanceledException;
}
