using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Services.Bff;
using Valora.Application.Access;

namespace Valora.Web.Controllers;

[Authorize, ApiController, AutoValidateAntiforgeryToken, Route("bff/diagnostics/{id:guid}")]
public sealed class BffDiagnosticOperationsController(
    IBffApiClient api,
    BffAuthenticationService authentication,
    IAuthorizationService authorization) : ControllerBase {

    [HttpGet("/bff/diagnostic-campaigns")]
    public async Task<IActionResult> Campaigns(CancellationToken ct) {
        try {
            return await ForwardPath("/api/v1/diagnostic-campaigns", false, ct);
        }
        catch (Exception ex) {
            return StatusCode(500, new { code = "DIAGNOSTIC_CAMPAIGNS_ERROR", message = "Erro ao carregar campanhas diagnósticas.", error = ex.Message, correlationId = HttpContext.TraceIdentifier });
        }
    }

    [HttpGet("participation")]
    public async Task<IActionResult> Participation(Guid id, CancellationToken ct) {
        try {
            return await Forward(id, "participation", false, ct);
        }
        catch (Exception ex) {
            return StatusCode(500, new { code = "PARTICIPATION_ERROR", message = "Erro ao carregar dados de participação.", error = ex.Message, correlationId = HttpContext.TraceIdentifier });
        }
    }

    [AcceptVerbs("GET", "POST"), Route("campaign/{command?}")]
    public async Task<IActionResult> Campaign(Guid id, string? command, CancellationToken ct) {
        try {
            if (HttpMethods.IsGet(Request.Method) && string.IsNullOrWhiteSpace(command)) {
                var canManage = (await authorization.AuthorizeAsync(User, null, ValoraPermissions.Campaigns.Manage)).Succeeded;
                var canDistribute = (await authorization.AuthorizeAsync(User, null, ValoraPermissions.Surveys.Distribute)).Succeeded;
                Response.Headers["X-Valora-Campaign-Manage"] = canManage ? "true" : "false";
                Response.Headers["X-Valora-Campaign-Distribute"] = canDistribute ? "true" : "false";
            }
            var isBodyRequired = HttpMethods.IsPost(Request.Method) && string.IsNullOrWhiteSpace(command);
            var resource = string.IsNullOrWhiteSpace(command) ? "campaign" : $"campaign/{command}";
            return await Forward(id, resource, isBodyRequired, ct);
        }
        catch (Exception ex) {
            return StatusCode(500, new { code = "CAMPAIGN_OPERATION_ERROR", message = "Erro ao processar operação da campanha.", error = ex.Message, correlationId = HttpContext.TraceIdentifier });
        }
    }

    [AcceptVerbs("GET", "POST"), Route("executive-report/{command?}")]
    public async Task<IActionResult> ExecutiveReport(Guid id, string? command, CancellationToken ct) {
        try {
            var resource = string.IsNullOrWhiteSpace(command) ? "executive-report" : $"executive-report/{command}";
            return await Forward(id, resource, false, ct);
        }
        catch (Exception ex) {
            return StatusCode(500, new { code = "EXECUTIVE_REPORT_ERROR", message = "Erro ao processar relatório executivo.", error = ex.Message, correlationId = HttpContext.TraceIdentifier });
        }
    }

    private async Task<IActionResult> Forward(Guid id, string resource, bool isBodyRequired, CancellationToken ct) {
        try {
            return await ForwardPath($"/api/v1/diagnostics/{id}/{resource}{Request.QueryString}", isBodyRequired, ct);
        }
        catch (Exception ex) {
            return StatusCode(500, new { code = "FORWARD_ERROR", message = "Erro no encaminhamento do diagnóstico.", error = ex.Message, correlationId = HttpContext.TraceIdentifier });
        }
    }

    private async Task<IActionResult> ForwardPath(string path, bool isBodyRequired, CancellationToken ct) {
        try {
            var session = await authentication.GetAsync(HttpContext, ct);
            if (session is null) {
                return Unauthorized(new { code = "SESSION_EXPIRED", message = "Sua sessão expirou. Entre novamente.", correlationId = HttpContext.TraceIdentifier });
            }

            var correlation = Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
            var (success, body, error) = await BffBodyReader.ReadJsonBodyAsync(Request, correlation, isBodyRequired: isBodyRequired, cancellationToken: ct);
            if (!success) {
                return error!;
            }

            using var response = await api.SendAsync(new HttpMethod(Request.Method), path, body, session.AccessToken, correlation, ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent) {
                Response.Headers["X-Correlation-Id"] = response.Headers.TryGetValues("X-Correlation-Id", out var v) ? v.First() : correlation;
                return NoContent();
            }

            var payload = await response.Content.ReadAsStringAsync(ct);
            if (response.Headers.TryGetValues("X-Valora-Module-Access", out var moduleAccess)) {
                Response.Headers["X-Valora-Module-Access"] = moduleAccess.First();
            }
            Response.Headers["X-Correlation-Id"] = response.Headers.TryGetValues("X-Correlation-Id", out var values) ? values.First() : correlation;

            return new ContentResult {
                StatusCode = (int)response.StatusCode,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
                Content = payload
            };
        }
        catch (OperationCanceledException) {
            return StatusCode(499, new { code = "CLIENT_CLOSED_REQUEST", message = "A requisição foi cancelada.", correlationId = HttpContext.TraceIdentifier });
        }
        catch (Exception ex) {
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "Erro ao processar a operação BFF.", error = ex.Message, correlationId = HttpContext.TraceIdentifier });
        }
    }
}
