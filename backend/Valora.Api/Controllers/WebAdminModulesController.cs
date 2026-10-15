using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common;
using Valora.Application.Contracts;
using Valora.Application.ModularSaas;
using Valora.Application.DTOs;
using Valora.Application.Security;

namespace Valora.Api.Controllers;

[Authorize]
[ApiController]
public sealed class WebAdminModulesController(
    ILogger<WebAdminModulesController> logger,
    IOrganizationRepository organizations,
    ISurveyRepository surveys,
    IResponseRepository responses,
    IAuditRepository audit,
    IPlanRepository plans,
    IFormRepository forms,
    IPlanEntitlementService entitlements,
    ICurrentRequestContext currentRequest, IPermissionService permissions, ICommercialSaasRepository commercial) : ControllerBase {
    private CurrentRequestContext Context => currentRequest.GetCurrent();
    private Guid OrganizationId => Context.EffectiveOrganizationId ?? Guid.Empty;
    private Guid? UserId => Context.UserId;
    private string CorrelationId => HttpContext.TraceIdentifier;

    private async Task<IActionResult> Safe(Func<Task<object?>> action, string actionName, string? surveyPermission = null) {
        if (OrganizationId == Guid.Empty) return StatusCode(StatusCodes.Status403Forbidden, new { status = 403, code = "ORGANIZATION_SCOPE_REQUIRED", message = "Selecione uma organização para acessar este recurso.", correlationId = CorrelationId });
        var permission = surveyPermission ?? (actionName switch {
            "surveys.list" or "surveys.get" or "links.list" => "surveys.read",
            "surveys.create" => "surveys.create",
            "surveys.update" => "surveys.update",
            "links.create" or "links.status" => "surveys.distribute",
            _ => null
        });
        if (permission is not null) {
            if (!Context.IsGlobalAdministrator && (UserId is not { } actor || !await permissions.HasPermissionAsync(actor, permission, OrganizationId)))
                return StatusCode(403, new { code = "PERMISSION_DENIED", message = "Você não tem permissão para executar esta ação.", correlationId = CorrelationId });
            var write = !HttpMethods.IsGet(Request.Method) && !HttpMethods.IsHead(Request.Method);
            var access = await commercial.EvaluateAccessAsync(OrganizationId, "surveys", write, HttpContext.RequestAborted);
            if (!access.Allowed || (write && access.ReadOnly))
                return StatusCode(403, new { code = access.Code, message = access.Message, correlationId = CorrelationId });
        }
        try { return Ok(await action()); }
        catch (ArgumentException ex) when (permission is not null) {
            return BadRequest(new { code = "SURVEY_VALIDATION_FAILED", message = ex.Message, correlationId = CorrelationId });
        }
        catch (InvalidOperationException ex) when (permission is not null) {
            return UnprocessableEntity(new { code = "SURVEY_NOT_AVAILABLE", message = ex.Message, correlationId = CorrelationId });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Falha controlada em endpoint administrativo web. Action={Action} OrganizationId={OrganizationId} CorrelationId={CorrelationId}", actionName, OrganizationId, CorrelationId);
            return StatusCode(500, new { ok = false, code = "WEB_ADMIN_ENDPOINT_ERROR", message = "Falha controlada ao processar a solicitação.", correlationId = CorrelationId });
        }
    }

    [ApiExplorerSettings(IgnoreApi = true), HttpGet("/legacy/organization/current")]
    public Task<IActionResult> GetOrganization() => Safe(async () => new { ok = true, data = await organizations.GetAsync(OrganizationId), correlationId = CorrelationId }, "organization.current.get");

    [ApiExplorerSettings(IgnoreApi = true), HttpPut("/legacy/organization/current")]
    public Task<IActionResult> PutOrganization([FromBody] UpdateOrganizationRequest request) => Safe(async () => { await organizations.UpdateCurrentAsync(OrganizationId, request); await Audit("organization.updated", "organization", OrganizationId); return new { ok = true, id = OrganizationId, correlationId = CorrelationId }; }, "organization.current.put");

    [ApiExplorerSettings(IgnoreApi = true), HttpGet("/legacy/organization/current/usage")]
    public Task<IActionResult> Usage() => Safe(async () => new { ok = true, data = await organizations.GetUsageAsync(OrganizationId), correlationId = CorrelationId }, "organization.usage");

    [HttpGet("/organization/current/limits")]
    public Task<IActionResult> Limits() => Safe(async () => { var planId = await plans.GetCurrentPlanIdAsync(OrganizationId) ?? "free"; var plan = await plans.GetByIdAsync(planId); return new { ok = true, planId, limits = plan?.Limits ?? new Dictionary<string, int>(), capabilities = plan?.Capabilities ?? new Dictionary<string, string>(), correlationId = CorrelationId }; }, "organization.limits");

    [HttpGet("/surveys")]
    public Task<IActionResult> Surveys() => Safe(async () => new { ok = true, data = await surveys.ListAdminAsync(OrganizationId), correlationId = CorrelationId }, "surveys.list");
    [HttpGet("/surveys/{surveyId:guid}")]
    public Task<IActionResult> Survey(Guid surveyId) => Safe(async () => new { ok = true, data = await surveys.GetAdminAsync(OrganizationId, surveyId), correlationId = CorrelationId }, "surveys.get");
    [HttpPost("/surveys")]
    public Task<IActionResult> CreateSurvey([FromBody] JsonElement request) => Safe(async () => { var id = await surveys.CreateAdminAsync(OrganizationId, GuidValue(request, "formId"), GuidValue(request, "formVersionId"), Text(request, "title") ?? "Novo diagnóstico", Text(request, "description"), DateValue(request, "startsAt"), DateValue(request, "expiresAt")); await Audit("survey.created", "survey", id); return new { ok = true, id, status = "draft", correlationId = CorrelationId }; }, "surveys.create");
    [HttpPut("/surveys/{surveyId:guid}")]
    public Task<IActionResult> UpdateSurvey(Guid surveyId, [FromBody] JsonElement request) => Safe(async () => { await surveys.UpdateAdminAsync(OrganizationId, surveyId, GuidValue(request, "formId"), GuidValue(request, "formVersionId"), Text(request, "title"), Text(request, "description"), DateValue(request, "startsAt"), DateValue(request, "expiresAt")); await Audit("survey.updated", "survey", surveyId); return new { ok = true, id = surveyId, correlationId = CorrelationId }; }, "surveys.update");
    [HttpPatch("/surveys/{surveyId:guid}/status")]
    public Task<IActionResult> SurveyStatus(Guid surveyId, [FromBody] JsonElement request) => Safe(async () => {
        var status = Text(request, "status") ?? "draft";
        if (status == "active") {
            var limit = await entitlements.CheckLimitAsync(OrganizationId, "activeSurveys");
            if (!limit.Allowed) throw new InvalidOperationException("Limite do plano atingido para diagnósticos ativos.");
        }
        await surveys.UpdateStatusAdminAsync(OrganizationId, surveyId, status);
        await Audit("survey.status", "survey", surveyId);
        return new { ok = true, id = surveyId, correlationId = CorrelationId };
    }, "surveys.status", Text(request, "status") == "closed" ? "surveys.close" : "surveys.publish");

    [HttpGet("/surveys/{surveyId:guid}/publication-review")]
    public Task<IActionResult> PublicationReview(Guid surveyId) => Safe(async () => {
        var survey = await surveys.GetAdminAsync(OrganizationId, surveyId);
        if (survey is null) throw new InvalidOperationException("Diagnóstico não encontrado.");

        var blockers = new List<object>();
        var warnings = new List<object>();

        string status = survey.status?.ToString() ?? "draft";
        if (status != "draft") {
            warnings.Add(new {
                problem = $"O diagnóstico já se encontra na situação '{status}'.",
                impact = "A publicação se aplica primariamente a diagnósticos em rascunho.",
                action = "Nenhuma ação de publicação é necessária.",
                targetUrl = $"/Surveys/{surveyId}",
                requiredPermission = "surveys.read",
                isBlocker = false
            });
        }

        Guid? formId = survey.form_id is Guid fid ? fid : (Guid.TryParse(survey.form_id?.ToString(), out Guid gFid) ? gFid : (Guid?)null);
        Guid? formVersionId = survey.form_version_id is Guid fvid ? fvid : (Guid.TryParse(survey.form_version_id?.ToString(), out Guid gFvid) ? gFvid : (Guid?)null);
        if (formVersionId is null || formVersionId == Guid.Empty) {
            blockers.Add(new {
                problem = "O diagnóstico não possui uma versão de formulário associada.",
                impact = "Não é possível publicar ou coletar sem um questionário definido.",
                action = "Selecione e vincule uma versão publicada do formulário.",
                targetUrl = $"/Surveys/{surveyId}/Edit",
                requiredPermission = "surveys.update",
                isBlocker = true
            });
        } else {
            var form = formId.HasValue ? await forms.GetPublishedVersionAsync(formId.Value, formVersionId.Value, OrganizationId) : null;
            if (form is null) {
                blockers.Add(new {
                    problem = "A versão do formulário vinculada não está publicada ou não pertence a esta organização.",
                    impact = "Participantes não conseguirão acessar as perguntas para responder.",
                    action = "Vincule uma versão publicada válida ou publique o formulário correspondente.",
                    targetUrl = "/Admin/Forms",
                    requiredPermission = "forms.publish",
                    isBlocker = true
                });
            } else {
                var questions = await forms.GetQuestionsAsync(formVersionId.Value);
                if (questions.Count == 0) {
                    blockers.Add(new {
                        problem = "O questionário vinculado não possui perguntas públicas ativas.",
                        impact = "O participante encontrará um formulário vazio e não conseguirá concluir a resposta.",
                        action = "Adicione ao menos uma pergunta ao formulário e publique nova versão.",
                        targetUrl = $"/Admin/Forms/{form.Id}",
                        requiredPermission = "forms.edit",
                        isBlocker = true
                    });
                } else {
                    bool allQualitative = questions.All(q => q.Type is "short_text" or "long_text" or "text_short" or "text_long" or "texto_curto" or "texto_longo");
                    if (allQualitative) {
                        warnings.Add(new {
                            problem = "Formulário exclusivamente qualitativo.",
                            impact = "As respostas serão salvas como relatos e evidências sem produzir pontuação numérica artificial ou radar de maturidade.",
                            action = "Aviso metodológico: se desejar cálculo quantitativo, adicione perguntas de escala, likert ou múltipla escolha.",
                            targetUrl = $"/Admin/Forms/{form.Id}",
                            requiredPermission = "forms.edit",
                            isBlocker = false
                        });
                    }
                }
            }
        }

        if (survey.expires_at is DateTime exp && exp <= DateTime.UtcNow) {
            blockers.Add(new {
                problem = "O período de coleta do diagnóstico já expirou.",
                impact = "Participantes receberão aviso de link expirado.",
                action = "Ajuste a data de término nas configurações do diagnóstico antes de ativar.",
                targetUrl = $"/Surveys/{surveyId}/Edit",
                requiredPermission = "surveys.update",
                isBlocker = true
            });
        }

        var limit = await entitlements.CheckLimitAsync(OrganizationId, "activeSurveys");
        if (!limit.Allowed && status == "draft") {
            blockers.Add(new {
                problem = "Limite comercial de diagnósticos ativos atingido para o plano atual.",
                impact = "A ativação do diagnóstico será recusada pela plataforma.",
                action = "Encerre diagnósticos que já concluíram a coleta ou realize o upgrade do plano.",
                targetUrl = "/Settings/Billing",
                requiredPermission = "commercial.manage",
                isBlocker = true
            });
        }

        return new {
            ok = true,
            surveyId,
            title = survey.title?.ToString() ?? "Diagnóstico",
            canPublish = blockers.Count == 0,
            blockers,
            warnings,
            correlationId = CorrelationId
        };
    }, "surveys.review", "surveys.read");

    [HttpGet("/surveys/{surveyId:guid}/links")]
    public Task<IActionResult> Links(Guid surveyId) => Safe(async () => new { ok = true, data = await surveys.ListLinksAdminAsync(OrganizationId, surveyId), correlationId = CorrelationId }, "links.list");
    [HttpPost("/surveys/{surveyId:guid}/links")]
    public Task<IActionResult> CreateLink(Guid surveyId, [FromBody] JsonElement request) => Safe(async () => { var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant(); var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant(); var cleanUrl = CleanPublicUrl(Text(request, "publicUrl"), surveyId); var id = await surveys.CreateLinkAdminAsync(OrganizationId, surveyId, hash, cleanUrl, null); await Audit("survey_link.created", "survey_link", id); return new { ok = true, id, surveyId, publicUrl = $"{cleanUrl}?token={token}", status = "active", correlationId = CorrelationId }; }, "links.create");
    [HttpPatch("/survey-links/{linkId:guid}/status")]
    public Task<IActionResult> LinkStatus(Guid linkId, [FromBody] JsonElement request) => Safe(async () => { await surveys.UpdateLinkStatusAdminAsync(OrganizationId, linkId, Text(request, "status") ?? "inactive"); await Audit("survey_link.status", "survey_link", linkId); return new { ok = true, id = linkId, correlationId = CorrelationId }; }, "links.status");

    [HttpGet("/responses")]
    public Task<IActionResult> Responses() => Safe(async () => new { ok = true, data = await responses.ListAdminAsync(OrganizationId), correlationId = CorrelationId }, "responses.list");
    [HttpGet("/responses/{responseId:guid}")]
    public new Task<IActionResult> Response(Guid responseId) => Safe(async () => new { ok = true, data = await responses.GetAdminAsync(OrganizationId, responseId), correlationId = CorrelationId }, "responses.get");

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("/legacy/responses/{responseId:guid}/result")]
    public IActionResult LegacyResponseResult(Guid responseId) => StatusCode(410, new { ok = false, id = responseId, code = "LEGACY_ENDPOINT_REMOVED", message = "Use GET /responses/{responseId}/result." });

    [HttpGet("/audit/events")]
    public Task<IActionResult> AuditEvents() => Safe(async () => new { ok = true, data = await audit.ListAdminAsync(OrganizationId), correlationId = CorrelationId }, "audit.events");

    [HttpGet("/settings")]
    public Task<IActionResult> Settings() => Safe(async () => new { ok = true, data = await organizations.GetSettingsAsync(OrganizationId), correlationId = CorrelationId }, "settings.get");
    [HttpPut("/settings")]
    public Task<IActionResult> PutSettings([FromBody] Dictionary<string, object?> request) => Safe(async () => { await organizations.UpsertSettingsAsync(OrganizationId, request); await Audit("settings.updated", "settings", OrganizationId); return new { ok = true, correlationId = CorrelationId }; }, "settings.put");

    private async Task Audit(string action, string entityType, Guid entityId) => await audit.AddAsync(new AuditEntry(OrganizationId, UserId, action, entityType, entityId.ToString(), "Ação administrativa via Valora.Web", "{}"));
    private static string? Text(JsonElement e, string name) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var p) && p.ValueKind != JsonValueKind.Null ? p.ToString() : null;
    private static Guid? TryGuid(JsonElement e, string name) => Guid.TryParse(Text(e, name), out var g) ? g : null;
    private static Guid GuidValue(JsonElement e, string name) => TryGuid(e, name) ?? throw new ArgumentException($"{name} obrigatório");
    private static DateTimeOffset? DateValue(JsonElement e, string name) => DateTimeOffset.TryParse(Text(e, name), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var value) ? value : null;
    private static string CleanPublicUrl(string? value, Guid surveyId) {
        if (string.IsNullOrWhiteSpace(value)) return $"/public/surveys/{surveyId}";
        var path = value.Split('?', '#')[0];
        return Uri.TryCreate(path, UriKind.Relative, out _) && path.StartsWith('/') ? path : $"/public/surveys/{surveyId}";
    }
}
