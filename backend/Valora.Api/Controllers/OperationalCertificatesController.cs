using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using System.Security.Claims; using Valora.Application.Contracts;
namespace Valora.Api.Controllers;
[Authorize][ApiController]
public sealed class OperationalCertificatesController(ICertificateOperationalService certs,IEmailQueueService email):ControllerBase{ Guid Org=>Guid.Parse(User.FindFirstValue("organization_id")??Guid.Empty.ToString());
[HttpPost("/certificates/responses/{responseId:guid}/generate")] public async Task<IActionResult> Generate(Guid responseId){try{return Ok(new{ok=true,certificate=await certs.GenerateAsync(Org,responseId)});}catch(InvalidOperationException ex){return StatusCode(403,new{ok=false,code=ex.Message});}}
[HttpGet("/certificates")] public async Task<IActionResult> List()=>Ok(new{ok=true,data=await certs.ListAsync(Org)});
[HttpGet("/certificates/{id:guid}/download")] public async Task<IActionResult> Download(Guid id){var html=await certs.DownloadHtmlAsync(Org,id); return html is null?NotFound():Content(html,"text/html");}
[HttpPatch("/certificates/{id:guid}/revoke")] public async Task<IActionResult> Revoke(Guid id){await certs.RevokeAsync(Org,id); return Ok(new{ok=true});}
[HttpPost("/certificates/{certificateId:guid}/send-email")] public async Task<IActionResult> Send(Guid certificateId,[FromBody]dynamic body)=>Ok(new{ok=true,job=await email.QueueCertificateAsync(Org,certificateId,(string?)body?.toEmail??"dev@example.com")});}
