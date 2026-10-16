using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Services.Bff;

namespace Valora.Web.Controllers;

[Authorize, ApiController, AutoValidateAntiforgeryToken]
public sealed class BffAssistedOperationsController(IBffApiClient api, BffAuthenticationService authentication) : ControllerBase {
    [Route("bff/support/{**path}"), Route("bff/feedback/{**path}"), Route("bff/customer-success/{**path}"), Route("bff/usage-analytics/{**path}"), Route("bff/onboarding/{**path}"), Route("bff/upgrade-requests/{**path}"), Route("bff/incidents/{**path}"), Route("bff/release-notes/{**path}"), Route("bff/data-quality/{**path}"), Route("bff/product-backlog/{**path}"), Route("bff/operations/{**path}")]
    [AcceptVerbs("GET", "POST", "PATCH")]
    public async Task<IActionResult> Forward(string? path, CancellationToken ct) {
        var session = await authentication.GetAsync(HttpContext, ct);
        if (session is null)
            return Unauthorized(new { code = "SESSION_EXPIRED", message = "Sua sessão expirou. Entre novamente.", correlationId = HttpContext.TraceIdentifier });

        var correlation = Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var (success, body, error) = await BffBodyReader.ReadJsonBodyAsync(Request, correlation, isBodyRequired: false, cancellationToken: ct);
        if (!success) return error!;

        var prefix = Request.Path.Value!.Split('/', StringSplitOptions.RemoveEmptyEntries)[1];
        using var response = await authentication.SendAuthorizedAsync(HttpContext, new HttpMethod(Request.Method), $"/api/v1/{prefix}/{path}{Request.QueryString}", body, correlation, ct);
        if (response is null)
            return Unauthorized(new { code = "SESSION_EXPIRED", message = "Sua sessão expirou. Entre novamente.", correlationId = correlation });

        if (response.StatusCode == HttpStatusCode.NoContent) {
            Response.Headers["X-Correlation-Id"] = response.Headers.TryGetValues("X-Correlation-Id", out var v) ? v.First() : correlation;
            return NoContent();
        }

        return new ContentResult { StatusCode = (int)response.StatusCode, ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json", Content = await response.Content.ReadAsStringAsync(ct) };
    }
}
