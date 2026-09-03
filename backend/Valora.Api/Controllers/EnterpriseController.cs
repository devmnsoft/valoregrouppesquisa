using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Enterprise;
using Valora.Domain.Operations;

namespace Valora.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/enterprise")]
public sealed class EnterpriseController(EnterpriseService service, IConfiguration configuration, IHostEnvironment hostEnvironment, ILogger<EnterpriseController> logger) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
    private Guid? OrganizationId => Guid.TryParse(User.FindFirstValue("organization_id"), out var id) ? id : null;
    private bool IsValoraAdmin => User.IsInRole("admin_valora") || User.Claims.Any(x => (x.Type is ClaimTypes.Role or "role") && x.Value == "admin_valora");

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct) => IsValoraAdmin ? Ok(await service.SummaryAsync(ct)) : PermissionDenied();

    [HttpGet("release-info")]
    public IActionResult ReleaseInfo()
    {
        if (!IsValoraAdmin) return PermissionDenied();
        var configured = configuration["Valora:Environment"]?.ToLowerInvariant();
        var environment = configured is "development" or "homologation" or "production" ? configured : hostEnvironment.EnvironmentName.ToLowerInvariant();
        return Ok(new
        {
            product = "Valora Insight™", version = configuration["Valora:Version"] ?? "8.0.0",
            releaseDate = configuration["Valora:ReleaseDate"] ?? "2026-08-07", environment,
            build = configuration["Valora:BuildId"] ?? "local", isHomologation = environment == "homologation"
        });
    }

    [HttpGet("anonymity/check")]
    public IActionResult CheckAnonymity([FromQuery] bool anonymous, [FromQuery] int responses, [FromQuery] int minimum = 5)
    {
        if (OrganizationId is null && !IsValoraAdmin) return PermissionDenied();
        var allowed = AnonymityPolicy.CanExposeSegment(anonymous, responses, minimum);
        return Ok(new { allowed, individualAllowed = AnonymityPolicy.CanExposeIndividual(anonymous), message = allowed ? null : AnonymityPolicy.InsufficientDataMessage });
    }

    [HttpGet("companies")]
    public async Task<IActionResult> Companies([FromQuery] EnterpriseListQuery query, CancellationToken ct)
    {
        if (!IsValoraAdmin) return PermissionDenied();
        if (query.From is not null && query.To is not null && query.To < query.From)
            return BadRequest(new { code = "INVALID_PERIOD", message = "A data final deve ser igual ou posterior à data inicial.", correlationId = HttpContext.TraceIdentifier });
        if (query.Page < 1 || query.PageSize is < 1 or > 100)
            return BadRequest(new { code = "INVALID_PAGINATION", message = "Informe uma página válida e até 100 empresas por página.", correlationId = HttpContext.TraceIdentifier });

        try
        {
            return Ok(await service.CompaniesAsync(query, ct));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falha ao listar empresas. CorrelationId={CorrelationId}", HttpContext.TraceIdentifier);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                code = "ENTERPRISE_COMPANIES_UNAVAILABLE",
                message = "Não foi possível carregar as empresas agora. Revise os filtros e tente novamente.",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    [HttpPatch("companies/{id:guid}/status")]
    public async Task<IActionResult> CompanyStatus(Guid id,[FromBody] ChangeStatusRequest request,CancellationToken ct)
    { if(!IsValoraAdmin)return PermissionDenied(); await service.ChangeCompanyStatusAsync(id,request.Status,UserId,ct); return NoContent(); }

    [HttpGet("crm/leads")]
    public async Task<IActionResult> Leads([FromQuery] EnterpriseListQuery query,CancellationToken ct)=>IsValoraAdmin?Ok(await service.LeadsAsync(query,ct)):PermissionDenied();

    [HttpPost("crm/leads")]
    public async Task<IActionResult> CreateLead([FromBody] CreateLeadRequest request,CancellationToken ct)
    { if(!IsValoraAdmin)return PermissionDenied(); var id=await service.CreateLeadAsync(request.Name,request.CompanyName,request.Email,request.Phone,request.IntendedPlan,request.Owner,request.NextActionAt,request.Notes,UserId,ct); return Created($"/api/v1/enterprise/crm/leads/{id}",new{id}); }

    [HttpGet("items/{kind}")]
    public async Task<IActionResult> Items(string kind,CancellationToken ct)
    { var scope=IsValoraAdmin?null:OrganizationId; return Ok(await service.ItemsAsync(scope,kind,ct)); }

    [HttpPost("items")]
    public async Task<IActionResult> CreateItem([FromBody] UpsertEnterpriseItemRequest request,CancellationToken ct)
    { if(!IsValoraAdmin && IsValoraOnly(request.Kind))return PermissionDenied(); var id=await service.SaveItemAsync(IsValoraAdmin?null:OrganizationId,null,request,UserId,ct); return Created($"/api/v1/enterprise/items/{id}",new{id}); }

    [HttpPut("items/{id:guid}")]
    public async Task<IActionResult> UpdateItem(Guid id,[FromBody] UpsertEnterpriseItemRequest request,CancellationToken ct)
    { if(!IsValoraAdmin && IsValoraOnly(request.Kind))return PermissionDenied(); await service.SaveItemAsync(IsValoraAdmin?null:OrganizationId,id,request,UserId,ct); return NoContent(); }

    [HttpPost("api-keys")]
    public async Task<IActionResult> ApiKey([FromBody] CreateApiKeyRequest request,CancellationToken ct)
    { return StatusCode(410,new{code="ENDPOINT_MOVED",message="Use /api/v1/api-keys. A criação exige entitlement Enterprise.",correlationId=HttpContext.TraceIdentifier}); }

    [RequestSizeLimit(2_000_000), HttpPost("imports/{type}/preview")]
    public IActionResult Preview(string type,[FromBody] CsvPreviewRequest request)
    { if(OrganizationId is null && !IsValoraAdmin)return PermissionDenied(); return Ok(service.PreviewCsv(type,request.Content)); }

    private static bool IsValoraOnly(string kind) => kind is "plan" or "template" or "automation" or "implementation"
        or "production-checklist" or "backup" or "release-note" or "data-quality" or "permission-governance"
        or "plan-governance" or "lgpd-request";
    private ObjectResult PermissionDenied()=>StatusCode(403,new{code="PERMISSION_DENIED",message="Seu perfil não possui permissão para acessar esta área.",correlationId=HttpContext.TraceIdentifier});
    public sealed record ChangeStatusRequest(string Status);
    public sealed record CreateLeadRequest(string Name,string? CompanyName,string? Email,string? Phone,string? IntendedPlan,string? Owner,DateTime? NextActionAt,string? Notes);
    public sealed record CreateApiKeyRequest(string Name,IReadOnlyList<string> Scopes);
    public sealed record CsvPreviewRequest(string Content);
}
