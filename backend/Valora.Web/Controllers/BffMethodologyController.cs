using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Services.Bff;

namespace Valora.Web.Controllers;

[Authorize, ApiController, AutoValidateAntiforgeryToken, Route("bff/methodology")]
public sealed class BffMethodologyController(IBffApiClient api, BffAuthenticationService authentication) : ControllerBase
{
    [AcceptVerbs("GET", "POST", "PATCH")]
    [Route("{**resource}")]
    public async Task<IActionResult> Forward(string? resource, CancellationToken ct)
    {
        var session = await authentication.GetAsync(HttpContext, ct);
        if (session is null) return Unauthorized(new { code = "SESSION_EXPIRED", message = "Sua sessão expirou. Entre novamente." });
        object? body = null;
        if (Request.ContentLength > 0) body = await JsonSerializer.DeserializeAsync<JsonElement>(Request.Body, cancellationToken: ct);
        var correlationId = Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        using var response = await api.SendAsync(new HttpMethod(Request.Method), $"/api/v1/methodology/{resource}{Request.QueryString}", body, session.AccessToken, correlationId, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        return new ContentResult { StatusCode = (int)response.StatusCode, ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json", Content = payload };
    }
}
