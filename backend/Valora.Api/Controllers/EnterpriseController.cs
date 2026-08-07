using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Enterprise;

namespace Valora.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/enterprise")]
public sealed class EnterpriseController(EnterpriseService service) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
    private Guid? OrganizationId => Guid.TryParse(User.FindFirstValue("organization_id"), out var id) ? id : null;
    private bool IsValoraAdmin => User.IsInRole("admin_valora") || User.Claims.Any(x => (x.Type is ClaimTypes.Role or "role") && x.Value == "admin_valora");

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct) => IsValoraAdmin ? Ok(await service.SummaryAsync(ct)) : PermissionDenied();

    [HttpGet("companies")]
    public async Task<IActionResult> Companies([FromQuery] EnterpriseListQuery query, CancellationToken ct) => IsValoraAdmin ? Ok(await service.CompaniesAsync(query,ct)) : PermissionDenied();

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
    { if(!IsValoraAdmin && request.Kind is "plan" or "template" or "automation")return PermissionDenied(); var id=await service.SaveItemAsync(IsValoraAdmin?null:OrganizationId,null,request,UserId,ct); return Created($"/api/v1/enterprise/items/{id}",new{id}); }

    [HttpPut("items/{id:guid}")]
    public async Task<IActionResult> UpdateItem(Guid id,[FromBody] UpsertEnterpriseItemRequest request,CancellationToken ct)
    { if(!IsValoraAdmin && request.Kind is "plan" or "template" or "automation")return PermissionDenied(); await service.SaveItemAsync(IsValoraAdmin?null:OrganizationId,id,request,UserId,ct); return NoContent(); }

    [HttpPost("api-keys")]
    public async Task<IActionResult> ApiKey([FromBody] CreateApiKeyRequest request,CancellationToken ct)
    { if(OrganizationId is not Guid organizationId)return PermissionDenied(); return Ok(await service.CreateApiKeyAsync(organizationId,request.Name,request.Scopes,UserId,ct)); }

    [RequestSizeLimit(2_000_000), HttpPost("imports/{type}/preview")]
    public IActionResult Preview(string type,[FromBody] CsvPreviewRequest request)
    { if(OrganizationId is null)return PermissionDenied(); return Ok(service.PreviewCsv(type,request.Content)); }

    private ObjectResult PermissionDenied()=>StatusCode(403,new{code="PERMISSION_DENIED",message="Seu perfil não possui permissão para acessar esta área.",correlationId=HttpContext.TraceIdentifier});
    public sealed record ChangeStatusRequest(string Status);
    public sealed record CreateLeadRequest(string Name,string? CompanyName,string? Email,string? Phone,string? IntendedPlan,string? Owner,DateTime? NextActionAt,string? Notes);
    public sealed record CreateApiKeyRequest(string Name,IReadOnlyList<string> Scopes);
    public sealed record CsvPreviewRequest(string Content);
}
