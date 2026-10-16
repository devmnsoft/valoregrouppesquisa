using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Services.Bff;

namespace Valora.Web.Controllers;

[Authorize, ApiController, AutoValidateAntiforgeryToken, Route("bff/methodology")]
public sealed class BffMethodologyController(BffAuthenticationService authentication) : ControllerBase {
    [AcceptVerbs("GET", "POST", "PATCH")]
    [Route("{**resource}")]
    public async Task<IActionResult> Forward(string? resource, CancellationToken ct) {
        var session = await authentication.GetAsync(HttpContext, ct);
        if (session is null)
            return Unauthorized(new { code = "SESSION_EXPIRED", message = "Sua sessão expirou. Entre novamente.", correlationId = HttpContext.TraceIdentifier });

        var correlationId = Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var (success, body, error) = await BffBodyReader.ReadJsonBodyAsync(Request, correlationId, isBodyRequired: false, cancellationToken: ct);
        if (!success) return error!;

        using var response = await authentication.SendAuthorizedAsync(HttpContext, new HttpMethod(Request.Method),
            $"/api/v1/methodology/{resource}{Request.QueryString}", body, correlationId, ct);
        if (response is null)
            return Unauthorized(new { code = "SESSION_EXPIRED", message = "Sua sessão expirou. Entre novamente.", correlationId });

        if (response.StatusCode == HttpStatusCode.NoContent) {
            Response.Headers["X-Correlation-Id"] = response.Headers.TryGetValues("X-Correlation-Id", out var v) ? v.First() : correlationId;
            return NoContent();
        }

        var payload = await response.Content.ReadAsStringAsync(ct);
        return new ContentResult { StatusCode = (int)response.StatusCode, ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json", Content = payload };
    }
}
