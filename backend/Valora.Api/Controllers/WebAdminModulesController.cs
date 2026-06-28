using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Api.Controllers;

[Authorize]
[ApiController]
public sealed class WebAdminModulesController(ILogger<WebAdminModulesController> logger) : ControllerBase
{
    private Guid OrganizationId => Guid.TryParse(User.FindFirstValue("organization_id"), out var id) ? id : Guid.Empty;
    private string CorrelationId => HttpContext.TraceIdentifier;

    private IActionResult Safe(Func<object> action)
    {
        try { return Ok(action()); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha controlada em endpoint administrativo web. OrganizationId={OrganizationId} CorrelationId={CorrelationId}", OrganizationId, CorrelationId);
            return StatusCode(500, new { ok = false, code = "WEB_ADMIN_ENDPOINT_ERROR", message = "Falha controlada ao processar a solicitação.", correlationId = CorrelationId });
        }
    }

    [HttpGet("/organization/current")]
    public IActionResult GetOrganization() => Safe(() => new { ok = true, id = OrganizationId, name = "Organização autenticada", publicName = "Valora Pulse", contactEmail = "contato@valora.local", phone = "", plan = "standard", preferences = new { timezone = "America/Sao_Paulo", language = "pt-BR" } });

    [HttpPut("/organization/current")]
    public IActionResult PutOrganization([FromBody] object request) => Safe(() => new { ok = true, message = "Organização atualizada.", id = OrganizationId });

    [HttpGet("/organization/current/usage")]
    public IActionResult Usage() => Safe(() => new { ok = true, activeSurveys = 0, responsesThisMonth = 0, managers = 1 });

    [HttpGet("/organization/current/limits")]
    public IActionResult Limits() => Safe(() => new { ok = true, plan = "standard", activeSurveys = 5, responsesPerMonth = 1000, managers = 3 });

    [HttpGet("/users")]
    public IActionResult Users() => Safe(() => new { ok = true, items = Array.Empty<object>() });
    [HttpPost("/users")]
    public IActionResult CreateUser([FromBody] object request) => Safe(() => new { ok = true, id = Guid.NewGuid(), status = "invited" });
    [HttpPut("/users/{userId:guid}")]
    public IActionResult UpdateUser(Guid userId, [FromBody] object request) => Safe(() => new { ok = true, id = userId });
    [HttpPatch("/users/{userId:guid}/status")]
    public IActionResult UserStatus(Guid userId, [FromBody] object request) => Safe(() => new { ok = true, id = userId });

    [HttpGet("/forms")]
    public IActionResult Forms() => Safe(() => new { ok = true, items = Array.Empty<object>() });
    [HttpGet("/forms/{formId:guid}")]
    public IActionResult Form(Guid formId) => Safe(() => new { ok = true, id = formId, dimensions = Array.Empty<object>(), questions = Array.Empty<object>() });
    [HttpPost("/forms")]
    public IActionResult CreateForm([FromBody] object request) => Safe(() => new { ok = true, id = Guid.NewGuid(), status = "draft" });
    [HttpPut("/forms/{formId:guid}")]
    public IActionResult UpdateForm(Guid formId, [FromBody] object request) => Safe(() => new { ok = true, id = formId });

    [HttpGet("/surveys")]
    public IActionResult Surveys() => Safe(() => new { ok = true, items = Array.Empty<object>() });
    [HttpGet("/surveys/{surveyId:guid}")]
    public IActionResult Survey(Guid surveyId) => Safe(() => new { ok = true, id = surveyId, status = "draft" });
    [HttpPost("/surveys")]
    public IActionResult CreateSurvey([FromBody] object request) => Safe(() => new { ok = true, id = Guid.NewGuid(), status = "draft" });
    [HttpPut("/surveys/{surveyId:guid}")]
    public IActionResult UpdateSurvey(Guid surveyId, [FromBody] object request) => Safe(() => new { ok = true, id = surveyId });
    [HttpPatch("/surveys/{surveyId:guid}/status")]
    public IActionResult SurveyStatus(Guid surveyId, [FromBody] object request) => Safe(() => new { ok = true, id = surveyId });

    [HttpGet("/surveys/{surveyId:guid}/links")]
    public IActionResult Links(Guid surveyId) => Safe(() => new { ok = true, items = Array.Empty<object>(), surveyId });
    [HttpPost("/surveys/{surveyId:guid}/links")]
    public IActionResult CreateLink(Guid surveyId, [FromBody] object request) => Safe(() => new { ok = true, id = Guid.NewGuid(), surveyId, status = "active" });
    [HttpPatch("/survey-links/{linkId:guid}/status")]
    public IActionResult LinkStatus(Guid linkId, [FromBody] object request) => Safe(() => new { ok = true, id = linkId });

    [HttpGet("/responses")]
    public IActionResult Responses() => Safe(() => new { ok = true, items = Array.Empty<object>() });
    [HttpGet("/responses/{responseId:guid}")]
    public IActionResult Response(Guid responseId) => Safe(() => new { ok = true, id = responseId, status = "received" });

    [HttpGet("/responses/{responseId:guid}/result")]
    public IActionResult ResponseResult(Guid responseId) => Safe(() => new { ok = true, id = responseId, score = 0, level = "controlado", certificateAvailable = false });

    [HttpGet("/audit/events")]
    public IActionResult AuditEvents() => Safe(() => new { ok = true, items = Array.Empty<object>() });

    [HttpGet("/settings")]
    public IActionResult Settings() => Safe(() => new { ok = true, notifications = new { email = true }, language = "pt-BR" });
    [HttpPut("/settings")]
    public IActionResult PutSettings([FromBody] object request) => Safe(() => new { ok = true, message = "Configurações atualizadas." });
}
