using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Valora.Web.Services.Bff;

namespace Valora.Web.Controllers;

[Authorize, ApiController, AutoValidateAntiforgeryToken]
public sealed class BffAssistedOperationsController : ControllerBase {
    private readonly BffAuthenticationService authentication;

    [ActivatorUtilitiesConstructor]
    public BffAssistedOperationsController(BffAuthenticationService authentication) {
        this.authentication = authentication;
    }

    public BffAssistedOperationsController(IBffApiClient api, BffAuthenticationService authentication)
        : this(authentication) { _ = api; }

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
        try {
            using var response = await authentication.SendAuthorizedAsync(HttpContext, new HttpMethod(Request.Method), $"/api/v1/{prefix}/{path}{Request.QueryString}", body, correlation, ct);
            if (response is null)
                return Unauthorized(new { code = "SESSION_EXPIRED", message = "Sua sessão expirou. Entre novamente.", correlationId = correlation });

            var payload = await response.Content.ReadAsStringAsync(ct);
            Response.Headers["X-Correlation-Id"] = correlation;
            return new ContentResult { StatusCode = (int)response.StatusCode, ContentType = "application/json", Content = payload };
        }
        catch (OperationCanceledException) {
            if (ct.IsCancellationRequested) return new EmptyResult();
            return StatusCode(504, new { message = "Operação assistida levando mais tempo que o esperado. Tente novamente em instantes.", correlationId = correlation });
        }
        catch (BffApiUnavailableException) {
            return StatusCode(503, new { message = "Não foi possível concluir a operação agora. Seus dados permanecem seguros.", correlationId = correlation });
        }
    }
}
