using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.CommercialDelivery;

namespace Valora.Api.Controllers;

[ApiController, Authorize, Route("api/v1")]
public sealed class CommercialLeadsController(PublicCommercialService service) : ControllerBase
{
    [HttpGet("leads")]
    public async Task<IActionResult> List(string? status,string? level,string? plan,int page=1,int pageSize=25,CancellationToken ct=default)=>Ok(new{items=await service.ListAsync(status,level,plan,page,pageSize,ct),page,pageSize,correlationId=HttpContext.TraceIdentifier});
    [HttpGet("commercial/dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)=>Ok(new{summary=await service.DashboardAsync(ct),correlationId=HttpContext.TraceIdentifier});
    [HttpPatch("leads/{id:guid}")]
    public async Task<IActionResult> Update(Guid id,[FromBody] LeadStatusRequest request,CancellationToken ct)
    {try{return await service.UpdateStatusAsync(id,request.Status,request.AssignedTo,request.Reason,ct)?Ok(new{message="Lead atualizado.",correlationId=HttpContext.TraceIdentifier}):NotFound(new{code="LEAD_NOT_FOUND",message="Lead não encontrado ou já convertido.",correlationId=HttpContext.TraceIdentifier});}catch(ArgumentException ex){return BadRequest(new{code="VALIDATION_ERROR",message=ex.Message,correlationId=HttpContext.TraceIdentifier});}}
}
public sealed record LeadStatusRequest(string Status,Guid? AssignedTo,string? Reason);
