using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;

namespace Valora.Api.Controllers;

[Authorize, ApiController, Route("api/v1")]
public sealed class AssistedOperationsController(IAssistedOperationsRepository repository) : ControllerBase
{
    [HttpGet("support/tickets")] public Task<IActionResult> Tickets(CancellationToken ct) => List("tickets", ct);
    [HttpPost("support/tickets")] public Task<IActionResult> CreateTicket([FromBody] JsonElement body,CancellationToken ct)=>Create("tickets",body,ct);
    [HttpGet("support/tickets/{id:guid}")] public async Task<IActionResult> Ticket(Guid id,CancellationToken ct)=>(await repository.GetAsync("tickets",id,OrganizationScope(),ct)) is { } value?Ok(value):NotFound(Error("TICKET_NOT_FOUND","Chamado não encontrado."));
    [HttpPatch("support/tickets/{id:guid}")] public Task<IActionResult> UpdateTicket(Guid id,[FromBody]JsonElement body,CancellationToken ct)=>Update("tickets",id,body,"updated",ct);
    [HttpPost("support/tickets/{id:guid}/comments")] public Task<IActionResult> Comment(Guid id,[FromBody]JsonElement body,CancellationToken ct){var values=Values(body);values["ticket_id"]=id;return Create("comments",values,ct);}
    [HttpPost("support/tickets/{id:guid}/close")] public Task<IActionResult> Close(Guid id,[FromBody]JsonElement body,CancellationToken ct){var v=Values(body);if(!v.TryGetValue("resolution_summary",out var summary)||string.IsNullOrWhiteSpace(summary?.ToString()))return Task.FromResult<IActionResult>(BadRequest(Error("RESOLUTION_REQUIRED","Informe o resumo da resolução.")));v["status"]="closed";return Update("tickets",id,v,"closed",ct);}
    [HttpPost("support/tickets/{id:guid}/reopen")] public Task<IActionResult> Reopen(Guid id,[FromBody]JsonElement body,CancellationToken ct){var v=Values(body);if(!v.TryGetValue("reopen_reason",out var reason)||string.IsNullOrWhiteSpace(reason?.ToString()))return Task.FromResult<IActionResult>(BadRequest(Error("REOPEN_REASON_REQUIRED","Informe a justificativa da reabertura.")));v["status"]="reopened";return Update("tickets",id,v,"reopened",ct);}

    [HttpGet("feedback")] public Task<IActionResult> Feedback(CancellationToken ct)=>List("feedback",ct);
    [HttpPost("feedback")] public Task<IActionResult> CreateFeedback([FromBody]JsonElement body,CancellationToken ct)=>Create("feedback",body,ct);
    [HttpPatch("feedback/{id:guid}")] public Task<IActionResult> UpdateFeedback(Guid id,[FromBody]JsonElement body,CancellationToken ct)=>Update("feedback",id,body,"updated",ct);
    [HttpGet("customer-success/organizations")] public async Task<IActionResult> CustomerSuccess(CancellationToken ct)=>Ok(await repository.CustomerHealthAsync(OrganizationScope(),ct));
    [HttpGet("customer-success/organizations/{id:guid}")] public async Task<IActionResult> Customer(Guid id,CancellationToken ct){var rows=await repository.CustomerHealthAsync(IsPlatformAdmin()?id:OrganizationScope(),ct);return rows.FirstOrDefault() is {} row?Ok(row):NotFound(Error("ORGANIZATION_NOT_FOUND","Organização não encontrada."));}
    [HttpGet("customer-success/health-score")] public async Task<IActionResult> Scores(CancellationToken ct)=>Ok(await repository.CustomerHealthAsync(OrganizationScope(),ct));
    [HttpGet("usage-analytics")] public async Task<IActionResult> Usage(CancellationToken ct)=>Ok(await repository.UsageAsync(OrganizationScope(),ct));
    [HttpGet("usage-analytics/modules")] public async Task<IActionResult> Modules(CancellationToken ct)=>Ok((await repository.UsageAsync(OrganizationScope(),ct)).Select(x=>new{organizationId=x.GetValueOrDefault("organization_id"),blockedFeatures=x.GetValueOrDefault("blocked_features")}));
    [HttpGet("usage-analytics/conversion")] public async Task<IActionResult> Conversion(CancellationToken ct)=>Ok((await repository.UsageAsync(OrganizationScope(),ct)).Select(x=>new{organizationId=x.GetValueOrDefault("organization_id"),publicLinks=x.GetValueOrDefault("public_links"),responses=x.GetValueOrDefault("responses")}));
    [HttpGet("onboarding")] public Task<IActionResult> Onboarding(CancellationToken ct)=>List("onboarding",ct);
    [HttpPatch("onboarding/{id:guid}/steps/{stepCode}")] public Task<IActionResult> Step(Guid id,string stepCode,[FromBody]JsonElement body,CancellationToken ct)=>Update("onboarding",id,body,"step."+stepCode,ct);
    [HttpGet("upgrade-requests")] public Task<IActionResult> Upgrades(CancellationToken ct)=>List("upgrade-requests",ct);
    [HttpPost("upgrade-requests")] public Task<IActionResult> CreateUpgrade([FromBody]JsonElement body,CancellationToken ct)=>Create("upgrade-requests",body,ct);
    [HttpPatch("upgrade-requests/{id:guid}")] public Task<IActionResult> UpdateUpgrade(Guid id,[FromBody]JsonElement body,CancellationToken ct)=>Update("upgrade-requests",id,body,"updated",ct);
    [HttpGet("incidents")] public Task<IActionResult> Incidents(CancellationToken ct)=>List("incidents",ct);
    [HttpPost("incidents")] public Task<IActionResult> CreateIncident([FromBody]JsonElement body,CancellationToken ct)=>Create("incidents",body,ct);
    [HttpPatch("incidents/{id:guid}")] public Task<IActionResult> UpdateIncident(Guid id,[FromBody]JsonElement body,CancellationToken ct)=>Update("incidents",id,body,"updated",ct);
    [HttpPost("incidents/{id:guid}/resolve")] public Task<IActionResult> Resolve(Guid id,[FromBody]JsonElement body,CancellationToken ct){var v=Values(body);if(!v.ContainsKey("resolution_summary"))return Task.FromResult<IActionResult>(BadRequest(Error("RESOLUTION_REQUIRED","Informe o resumo da resolução.")));v["status"]="resolved";return Update("incidents",id,v,"resolved",ct);}
    [AllowAnonymous,HttpGet("release-notes")] public Task<IActionResult> Releases(CancellationToken ct)=>List("release-notes",ct);
    [HttpPost("release-notes")] public Task<IActionResult> CreateRelease([FromBody]JsonElement body,CancellationToken ct)=>Create("release-notes",body,ct);
    [HttpPatch("release-notes/{id:guid}")] public Task<IActionResult> UpdateRelease(Guid id,[FromBody]JsonElement body,CancellationToken ct)=>Update("release-notes",id,body,"updated",ct);
    [HttpPost("release-notes/{id:guid}/publish")] public Task<IActionResult> Publish(Guid id,[FromBody]JsonElement body,CancellationToken ct){var v=Values(body);if(!v.TryGetValue("content",out var content)||string.IsNullOrWhiteSpace(content?.ToString()))return Task.FromResult<IActionResult>(BadRequest(Error("CONTENT_REQUIRED","Release note sem conteúdo não pode ser publicada.")));v["status"]="published";v["published_at"]=DateTimeOffset.UtcNow;return Update("release-notes",id,v,"published",ct);}
    [HttpGet("data-quality")] public Task<IActionResult> DataQuality(CancellationToken ct)=>List("data-quality",ct);
    [HttpPost("data-quality/run")] public async Task<IActionResult> Run(CancellationToken ct)=>Accepted(new{id=await repository.RunDataQualityAsync(UserId(),HttpContext.TraceIdentifier,ct),correlationId=HttpContext.TraceIdentifier});

    private async Task<IActionResult> List(string resource,CancellationToken ct)=>Ok(await repository.ListAsync(resource,OrganizationScope(),ct));
    private Task<IActionResult> Create(string resource,JsonElement body,CancellationToken ct)=>Create(resource,Values(body),ct);
    private async Task<IActionResult> Create(string resource,IReadOnlyDictionary<string,object?> values,CancellationToken ct){var id=await repository.CreateAsync(resource,OrganizationScope(),UserId(),values,HttpContext.TraceIdentifier,ct);return Created($"/api/v1/{resource}/{id}",new{id,correlationId=HttpContext.TraceIdentifier});}
    private async Task<IActionResult> Update(string resource,Guid id,JsonElement body,string action,CancellationToken ct)=>await Update(resource,id,Values(body),action,ct);
    private async Task<IActionResult> Update(string resource,Guid id,IReadOnlyDictionary<string,object?> values,string action,CancellationToken ct)=>await repository.UpdateAsync(resource,id,OrganizationScope(),values,action,HttpContext.TraceIdentifier,ct)?Ok(new{id,correlationId=HttpContext.TraceIdentifier}):NotFound(Error("RESOURCE_NOT_FOUND","Registro não encontrado."));
    private Guid? OrganizationScope()=>IsPlatformAdmin()?null:Guid.TryParse(User.FindFirstValue("organization_id"),out var id)?id:Guid.Empty;
    private Guid? UserId()=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)??User.FindFirstValue("sub"),out var id)?id:null;
    private bool IsPlatformAdmin()=>User.IsInRole("admin_valora")||User.IsInRole("platform_admin");
    private object Error(string code,string message)=>new{code,message,correlationId=HttpContext.TraceIdentifier};
    private static Dictionary<string,object?> Values(JsonElement body)=>body.EnumerateObject().ToDictionary(x=>ToSnakeCase(x.Name),x=>(object?)Convert(x.Value),StringComparer.OrdinalIgnoreCase);
    private static object? Convert(JsonElement value)=>value.ValueKind switch{JsonValueKind.String when value.TryGetGuid(out var id)=>id,JsonValueKind.String when value.TryGetDateTimeOffset(out var date)=>date,JsonValueKind.String=>value.GetString(),JsonValueKind.True=>true,JsonValueKind.False=>false,JsonValueKind.Number when value.TryGetInt32(out var number)=>number,JsonValueKind.Null=>null,_=>value.Clone()};
    private static string ToSnakeCase(string value)=>string.Concat(value.Select((c,i)=>char.IsUpper(c)&&i>0?"_"+char.ToLowerInvariant(c):char.ToLowerInvariant(c).ToString()));
}
