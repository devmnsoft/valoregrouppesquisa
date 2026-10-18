using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Services.Bff;

namespace Valora.Web.Controllers;

[Authorize, ApiController, AutoValidateAntiforgeryToken, Route("bff/campaigns")]
public sealed class BffCampaignsController(
    IBffApiClient api,
    BffAuthenticationService authentication) : ControllerBase {

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) {
        try {
            return await ForwardPath($"/api/v1/campaigns{Request.QueryString}", false, ct);
        }
        catch (Exception ex) {
            return StatusCode(500, new { code = "CAMPAIGN_LIST_ERROR", message = "Erro ao listar campanhas.", error = ex.Message, correlationId = HttpContext.TraceIdentifier });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) {
        try {
            return await ForwardPath($"/api/v1/campaigns/{id}", false, ct);
        }
        catch (Exception ex) {
            return StatusCode(500, new { code = "CAMPAIGN_GET_ERROR", message = "Erro ao obter campanha.", error = ex.Message, correlationId = HttpContext.TraceIdentifier });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CancellationToken ct) {
        try {
            return await ForwardPath("/api/v1/campaigns", true, ct);
        }
        catch (Exception ex) {
            return StatusCode(500, new { code = "CAMPAIGN_CREATE_ERROR", message = "Erro ao criar campanha.", error = ex.Message, correlationId = HttpContext.TraceIdentifier });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CancellationToken ct) {
        try {
            return await ForwardPath($"/api/v1/campaigns/{id}", true, ct);
        }
        catch (Exception ex) {
            return StatusCode(500, new { code = "CAMPAIGN_UPDATE_ERROR", message = "Erro ao atualizar campanha.", error = ex.Message, correlationId = HttpContext.TraceIdentifier });
        }
    }

    [HttpPost("{id:guid}/{actionName}")]
    public async Task<IActionResult> Transition(Guid id, string actionName, CancellationToken ct) {
        try {
            return await ForwardPath($"/api/v1/campaigns/{id}/{actionName}", false, ct);
        }
        catch (Exception ex) {
            return StatusCode(500, new { code = "CAMPAIGN_TRANSITION_ERROR", message = $"Erro ao executar transição '{actionName}'.", error = ex.Message, correlationId = HttpContext.TraceIdentifier });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) {
        try {
            return await ForwardPath($"/api/v1/campaigns/{id}", false, ct);
        }
        catch (Exception ex) {
            return StatusCode(500, new { code = "CAMPAIGN_DELETE_ERROR", message = "Erro ao remover campanha.", error = ex.Message, correlationId = HttpContext.TraceIdentifier });
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
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "Erro ao processar a operação de campanhas.", error = ex.Message, correlationId = HttpContext.TraceIdentifier });
        }
    }
}
