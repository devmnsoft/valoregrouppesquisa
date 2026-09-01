using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.SolutionPacks;
namespace Valora.Api.Controllers;
[Authorize,ApiController,Route("api/v1/solution-packs")]
public sealed class SolutionPacksController(SolutionPackService packs,SolutionPackVersionService versions,SolutionPackInstallationService installations,SolutionPackDependencyService dependencies,SolutionPackRollbackService rollbacks,SolutionPackCatalogService catalog):ControllerBase
{
 private Guid OrganizationId=>Guid.TryParse(User.FindFirstValue("organization_id"),out var c)&&c!=Guid.Empty?c:Guid.TryParse(Request.Headers["X-Organization-Id"].FirstOrDefault(),out var h)?h:Guid.Empty;
 private Guid UserId=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:Guid.Empty;
 private ActionResult InvalidScope()=>BadRequest(new{code="ORGANIZATION_REQUIRED",message="Selecione uma organização válida.",correlationId=HttpContext.TraceIdentifier});
 [HttpGet] public async Task<ActionResult> List([FromQuery]string? segment,[FromQuery]string? category,CancellationToken ct)=>OrganizationId==Guid.Empty?InvalidScope():Ok(await packs.List(OrganizationId,segment,category,ct));
 [HttpGet("{id:guid}")] public async Task<ActionResult> Get(Guid id,CancellationToken ct){if(OrganizationId==Guid.Empty)return InvalidScope();var value=await packs.Get(id,OrganizationId,ct);return value is null?NotFound(new{code="PACK_NOT_FOUND",message="Pacote não encontrado."}):Ok(value);}
 [HttpPost] public async Task<ActionResult> Create(CreateSolutionPackRequest request,CancellationToken ct){if(OrganizationId==Guid.Empty)return InvalidScope();var id=await packs.Create(OrganizationId,UserId,request,ct);return CreatedAtAction(nameof(Get),new{id},new{id});}
 [HttpPost("{id:guid}/new-version")] public async Task<ActionResult> NewVersion(Guid id,NewVersionRequest request,CancellationToken ct)=>OrganizationId==Guid.Empty?InvalidScope():Ok(new{id=await versions.NewVersion(id,OrganizationId,UserId,request,ct)});
 [HttpPost("{id:guid}/publish")] public async Task<ActionResult> Publish(Guid id,CancellationToken ct)=>OrganizationId==Guid.Empty?InvalidScope():await versions.Publish(id,OrganizationId,UserId,ct)?Ok(new{message="Versão publicada."}):Conflict(new{code="NO_DRAFT_VERSION",message="Crie uma nova versão antes de publicar."});
 [HttpPost("{id:guid}/preview-installation")] public async Task<ActionResult> Preview(Guid id,[FromBody]Guid? versionId,CancellationToken ct)=>OrganizationId==Guid.Empty?InvalidScope():Ok(await dependencies.Preview(id,OrganizationId,versionId,ct));
 [HttpPost("{id:guid}/install")] public async Task<ActionResult> Install(Guid id,InstallSolutionPackRequest request,CancellationToken ct)=>OrganizationId==Guid.Empty?InvalidScope():Ok(new{id=await installations.Install(id,OrganizationId,UserId,request,ct)});
 [HttpPost("installations/{id:guid}/rollback")] public async Task<ActionResult> Rollback(Guid id,CancellationToken ct)=>OrganizationId==Guid.Empty?InvalidScope():await rollbacks.Rollback(id,OrganizationId,UserId,ct)?Ok(new{message="Rollback concluído e auditado."}):Conflict(new{code="ROLLBACK_UNAVAILABLE",message="Esta instalação não permite rollback."});
 [HttpGet("installations")] public async Task<ActionResult> Installations(CancellationToken ct)=>OrganizationId==Guid.Empty?InvalidScope():Ok(await installations.List(OrganizationId,ct));
 [HttpGet("updates")] public async Task<ActionResult> Updates(CancellationToken ct)=>OrganizationId==Guid.Empty?InvalidScope():Ok(await catalog.Updates(OrganizationId,ct));
}
