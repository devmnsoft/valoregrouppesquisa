using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Valora.Web.Services.Bff;

namespace Valora.Web.Controllers;

[Authorize, ApiController, AutoValidateAntiforgeryToken, Route("bff/diagnostics/{id:guid}/workspace")]
public sealed class BffDiagnosticWorkspaceController : ControllerBase {
    private readonly BffAuthenticationService authentication;

    [ActivatorUtilitiesConstructor]
    public BffDiagnosticWorkspaceController(BffAuthenticationService authentication) {
        this.authentication = authentication;
    }

    public BffDiagnosticWorkspaceController(IBffApiClient api, BffAuthenticationService authentication)
        : this(authentication) { _ = api; }

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
        try {
            using var response = await authentication.SendAuthorizedAsync(HttpContext, new HttpMethod(Request.Method),
                $"/api/v1/diagnostics/{id}/workspace{suffix}{Request.QueryString}", body, correlation, ct);
            if (response is null)
                return Unauthorized(new { code = "SESSION_EXPIRED", message = "Sua sessão expirou. Entre novamente.", correlationId = correlation });

            var payload = await response.Content.ReadAsStringAsync(ct);
            Response.Headers["X-Correlation-Id"] = correlation;
            return new ContentResult { StatusCode = (int)response.StatusCode, ContentType = "application/json", Content = payload };
        }
        catch (OperationCanceledException) {
            if (ct.IsCancellationRequested) return new EmptyResult();
            return StatusCode(504, new { message = "O Workspace Diagnóstico está levando mais tempo que o esperado. Tente novamente em instantes.", correlationId = correlation });
        }
        catch (BffApiUnavailableException) {
            return StatusCode(503, new { message = "Não foi possível atualizar o Workspace Diagnóstico agora. Seus dados permanecem seguros.", correlationId = correlation });
        }
    }
}
