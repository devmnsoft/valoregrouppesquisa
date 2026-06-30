using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Security;

namespace Valora.Api.Controllers;

[Authorize]
[ApiController]
public sealed class WebAdminModulesController(
    ILogger<WebAdminModulesController> logger,
    IOrganizationRepository organizations,
    IUserRepository users,
    IFormRepository forms,
    ISurveyRepository surveys,
    IResponseRepository responses,
    IAuditRepository audit,
    IPlanRepository plans,
    IPasswordHasher passwordHasher) : ControllerBase
{
    private Guid OrganizationId => Guid.TryParse(User.FindFirstValue("organization_id"), out var id) ? id : Guid.Empty;
    private Guid? UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private string CorrelationId => HttpContext.TraceIdentifier;

    private async Task<IActionResult> Safe(Func<Task<object?>> action, string actionName)
    {
        if (OrganizationId == Guid.Empty) return Unauthorized(new { ok = false, code = "ORGANIZATION_SCOPE_REQUIRED", correlationId = CorrelationId });
        try { return Ok(await action()); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha controlada em endpoint administrativo web. Action={Action} OrganizationId={OrganizationId} CorrelationId={CorrelationId}", actionName, OrganizationId, CorrelationId);
            return StatusCode(500, new { ok = false, code = "WEB_ADMIN_ENDPOINT_ERROR", message = "Falha controlada ao processar a solicitação.", correlationId = CorrelationId });
        }
    }

    [HttpGet("/organization/current")]
    public Task<IActionResult> GetOrganization() => Safe(async () => new { ok = true, data = await organizations.GetAsync(OrganizationId), correlationId = CorrelationId }, "organization.current.get");

    [HttpPut("/organization/current")]
    public Task<IActionResult> PutOrganization([FromBody] UpdateOrganizationRequest request) => Safe(async () => { await organizations.UpdateCurrentAsync(OrganizationId, request); await Audit("organization.updated", "organization", OrganizationId); return new { ok = true, id = OrganizationId, correlationId = CorrelationId }; }, "organization.current.put");

    [HttpGet("/organization/current/usage")]
    public Task<IActionResult> Usage() => Safe(async () => new { ok = true, data = await organizations.GetUsageAsync(OrganizationId), correlationId = CorrelationId }, "organization.usage");

    [HttpGet("/organization/current/limits")]
    public Task<IActionResult> Limits() => Safe(async () => { var planId = await plans.GetCurrentPlanIdAsync(OrganizationId) ?? "free"; var plan = await plans.GetByIdAsync(planId); return new { ok = true, planId, limits = plan?.Limits ?? new Dictionary<string,int>(), capabilities = plan?.Capabilities ?? new Dictionary<string,string>(), correlationId = CorrelationId }; }, "organization.limits");

    [HttpGet("/users")]
    public Task<IActionResult> Users() => Safe(async () => new { ok = true, data = await users.ListByOrganizationAsync(OrganizationId), correlationId = CorrelationId }, "users.list");
    [HttpPost("/users")]
    public Task<IActionResult> CreateUser([FromBody] JsonElement request) => Safe(async () => { var name = Text(request,"name") ?? "Usuário"; var email = Text(request,"email") ?? throw new ArgumentException("email obrigatório"); var role = Text(request,"role") ?? "empresa_admin"; var temp = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)); var id = await users.CreateAsync(OrganizationId, name, email, passwordHasher.Hash(temp), role); await Audit("user.created", "user", id); return new { ok = true, id, status = "active", correlationId = CorrelationId }; }, "users.create");
    [HttpPut("/users/{userId:guid}")]
    public Task<IActionResult> UpdateUser(Guid userId, [FromBody] JsonElement request) => Safe(async () => { await users.UpdateAsync(OrganizationId, userId, Text(request,"name"), Text(request,"email"), Text(request,"role"), Text(request,"phone")); await Audit("user.updated", "user", userId); return new { ok = true, id = userId, correlationId = CorrelationId }; }, "users.update");
    [HttpPatch("/users/{userId:guid}/status")]
    public Task<IActionResult> UserStatus(Guid userId, [FromBody] JsonElement request) => Safe(async () => { await users.UpdateStatusAsync(OrganizationId, userId, Text(request,"status") ?? "inactive"); await Audit("user.status", "user", userId); return new { ok = true, id = userId, correlationId = CorrelationId }; }, "users.status");

    [HttpGet("/forms")]
    public Task<IActionResult> Forms() => Safe(async () => new { ok = true, data = await forms.ListAdminAsync(OrganizationId), correlationId = CorrelationId }, "forms.list");
    [HttpGet("/forms/{formId:guid}")]
    public Task<IActionResult> Form(Guid formId) => Safe(async () => new { ok = true, data = await forms.GetByIdAsync(formId), questions = await forms.GetQuestionsAsync(formId), options = await forms.GetQuestionOptionsAsync(formId), correlationId = CorrelationId }, "forms.get");
    [HttpPost("/forms")]
    public Task<IActionResult> CreateForm([FromBody] JsonElement request) => Safe(async () => { var id = await forms.CreateAdminAsync(OrganizationId, Text(request,"name") ?? "Novo formulário", Text(request,"description"), Text(request,"status") ?? "draft"); await Audit("form.created", "form", id); return new { ok = true, id, status = "draft", correlationId = CorrelationId }; }, "forms.create");
    [HttpPut("/forms/{formId:guid}")]
    public Task<IActionResult> UpdateForm(Guid formId, [FromBody] JsonElement request) => Safe(async () => { await forms.UpdateAdminAsync(OrganizationId, formId, Text(request,"name"), Text(request,"description"), Text(request,"status")); await Audit("form.updated", "form", formId); return new { ok = true, id = formId, correlationId = CorrelationId }; }, "forms.update");

    [HttpGet("/surveys")]
    public Task<IActionResult> Surveys() => Safe(async () => new { ok = true, data = await surveys.ListAdminAsync(OrganizationId), correlationId = CorrelationId }, "surveys.list");
    [HttpGet("/surveys/{surveyId:guid}")]
    public Task<IActionResult> Survey(Guid surveyId) => Safe(async () => new { ok = true, data = await surveys.GetAdminAsync(OrganizationId, surveyId), correlationId = CorrelationId }, "surveys.get");
    [HttpPost("/surveys")]
    public Task<IActionResult> CreateSurvey([FromBody] JsonElement request) => Safe(async () => { var id = await surveys.CreateAdminAsync(OrganizationId, GuidValue(request,"formId"), Text(request,"title") ?? "Nova pesquisa", Text(request,"description"), Text(request,"status") ?? "draft"); await Audit("survey.created", "survey", id); return new { ok = true, id, status = "draft", correlationId = CorrelationId }; }, "surveys.create");
    [HttpPut("/surveys/{surveyId:guid}")]
    public Task<IActionResult> UpdateSurvey(Guid surveyId, [FromBody] JsonElement request) => Safe(async () => { await surveys.UpdateAdminAsync(OrganizationId, surveyId, TryGuid(request,"formId"), Text(request,"title"), Text(request,"description"), Text(request,"status")); await Audit("survey.updated", "survey", surveyId); return new { ok = true, id = surveyId, correlationId = CorrelationId }; }, "surveys.update");
    [HttpPatch("/surveys/{surveyId:guid}/status")]
    public Task<IActionResult> SurveyStatus(Guid surveyId, [FromBody] JsonElement request) => Safe(async () => { await surveys.UpdateStatusAdminAsync(OrganizationId, surveyId, Text(request,"status") ?? "draft"); await Audit("survey.status", "survey", surveyId); return new { ok = true, id = surveyId, correlationId = CorrelationId }; }, "surveys.status");

    [HttpGet("/surveys/{surveyId:guid}/links")]
    public Task<IActionResult> Links(Guid surveyId) => Safe(async () => new { ok = true, data = await surveys.ListLinksAdminAsync(OrganizationId, surveyId), correlationId = CorrelationId }, "links.list");
    [HttpPost("/surveys/{surveyId:guid}/links")]
    public Task<IActionResult> CreateLink(Guid surveyId, [FromBody] JsonElement request) => Safe(async () => { var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant(); var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant(); var url = Text(request,"publicUrl") ?? $"/Public/Survey?survey={surveyId}&token={token}"; var id = await surveys.CreateLinkAdminAsync(OrganizationId, surveyId, hash, url, null); await Audit("survey_link.created", "survey_link", id); return new { ok = true, id, surveyId, publicUrl = url, status = "active", correlationId = CorrelationId }; }, "links.create");
    [HttpPatch("/survey-links/{linkId:guid}/status")]
    public Task<IActionResult> LinkStatus(Guid linkId, [FromBody] JsonElement request) => Safe(async () => { await surveys.UpdateLinkStatusAdminAsync(OrganizationId, linkId, Text(request,"status") ?? "inactive"); await Audit("survey_link.status", "survey_link", linkId); return new { ok = true, id = linkId, correlationId = CorrelationId }; }, "links.status");

    [HttpGet("/responses")]
    public Task<IActionResult> Responses() => Safe(async () => new { ok = true, data = await responses.ListAdminAsync(OrganizationId), correlationId = CorrelationId }, "responses.list");
    [HttpGet("/responses/{responseId:guid}")]
    public Task<IActionResult> Response(Guid responseId) => Safe(async () => new { ok = true, data = await responses.GetAdminAsync(OrganizationId, responseId), correlationId = CorrelationId }, "responses.get");

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
}
