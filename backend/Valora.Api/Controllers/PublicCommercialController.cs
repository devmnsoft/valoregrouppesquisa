using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.CommercialDelivery;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class PublicCommercialController(PublicCommercialService service) : ControllerBase
{
    private string CorrelationId => HttpContext.TraceIdentifier;

    [AllowAnonymous, HttpGet("/api/v1/public/portal")]
    public IActionResult Portal() => Ok(new { product="Valora Insight™", diagnostic="Diagnóstico Estratégico de Maturidade Organizacional", templateCode="VALORA_STRATEGIC_MATURITY_V1", duration="15-20 minutos", free=true });

    [AllowAnonymous, HttpGet("/api/v1/public/plans")]
    public IActionResult Plans() => Ok(new { priceMessage="Planos comerciais sob consulta, conforme escopo e porte da organização.", items=new[]{new{code="free",name="Gratuito"},new{code="professional",name="Profissional"},new{code="enterprise",name="Enterprise"}} });

    [AllowAnonymous]
    [HttpPost("/api/v1/public/leads")]
    [HttpPost("/api/v1/public/diagnostic/start")]
    public async Task<IActionResult> Start([FromBody] PublicLeadRequest request, CancellationToken ct)
    {
        try { var result=await service.StartAsync(request,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString(),ct); return Created($"/api/v1/public/diagnostic/{result.SessionId}/result",new{leadId=result.LeadId,sessionId=result.SessionId,status=result.Status,correlationId=CorrelationId}); }
        catch(ArgumentException ex){return BadRequest(new{code="VALIDATION_ERROR",message=ex.Message,correlationId=CorrelationId});}
    }

    [AllowAnonymous]
    [HttpPost("/api/v1/public/contact-requests")]
    [HttpPost("/api/v1/public/plan-interest")]
    public async Task<IActionResult> Contact([FromBody] CommercialRequestInput request,CancellationToken ct)
    { try { var id=await service.RequestContactAsync(request,ct); return Created($"/api/v1/public/contact-requests/{id}",new{id,status="requested",message="Solicitação registrada. O plano não foi alterado.",correlationId=CorrelationId}); } catch(ArgumentException ex){return BadRequest(new{code="VALIDATION_ERROR",message=ex.Message,correlationId=CorrelationId});} }
}
