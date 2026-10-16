using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Services.Bff;

namespace Valora.Web.Controllers;

[Authorize, ApiController, AutoValidateAntiforgeryToken, Route("bff/diagnostics/{id:guid}/workspace")]
public sealed class BffDiagnosticWorkspaceController(IBffApiClient api, BffAuthenticationService authentication) : ControllerBase {
    [AcceptVerbs("GET", "POST", "PUT", "PATCH"), Route("{**resource}")]
    public async Task<IActionResult> Forward(Guid id, string? resource, CancellationToken ct) {
        var session = await authentication.GetAsync(HttpContext, ct);
        if (session is null)
            return Unauthorized(new { code = "SESSION_EXPIRED", message = "Sua sessão expirou. Entre novamente.", correlationId = HttpContext.TraceIdentifier });

        var correlation = Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var suffix = string.IsNullOrWhiteSpace(resource) ? string.Empty : "/" + resource;
        var isBodyRequired = HttpMethods.IsPut(Request.Method);
        var (success, body, error) = await BffBodyReader.ReadJsonBodyAsync(Request, correlation, isBodyRequired: isBodyRequired, cancellationToken: ct);
        if (!success) return error!;
        using var response = await authentication.SendAuthorizedAsync(HttpContext, new HttpMethod(Request.Method),
            $"/api/v1/diagnostics/{id}/workspace{suffix}{Request.QueryString}", body, correlation, ct);
        if (response is null)
            return Unauthorized(new { code = "SESSION_EXPIRED", message = "Sua sessão expirou. Entre novamente.", correlationId = correlation });

        if (response.StatusCode == HttpStatusCode.NoContent) {
            Response.Headers["X-Correlation-Id"] = response.Headers.TryGetValues("X-Correlation-Id", out var v) ? v.First() : correlation;
            return NoContent();
        }

        var payload = await response.Content.ReadAsStringAsync(ct);
        Response.Headers["X-Correlation-Id"] = response.Headers.TryGetValues("X-Correlation-Id", out var values) ? values.First() : correlation;
        return new ContentResult { StatusCode = (int)response.StatusCode, ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json", Content = payload };
    }
}
