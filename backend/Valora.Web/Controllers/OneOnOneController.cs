using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.OneOnOne;

namespace Valora.Web.Controllers;

[Authorize,Route("OneOnOne")]
public sealed class OneOnOneController(OneOnOneSessionService sessions,CreateOneOnOneSessionUseCase create,GenerateOneOnOneAgendaUseCase agenda,CompleteOneOnOneSessionUseCase complete,RegisterOneOnOneCommitmentUseCase commitments,RegisterLeadershipFeedbackUseCase feedback,IOneOnOneRepository repository):Controller
{
    Guid O=>Guid.TryParse(User.FindFirstValue("organization_id")??User.FindFirstValue("organizationId"),out var x)?x:Guid.Empty;
    Guid U=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)??User.FindFirstValue("sub"),out var x)?x:Guid.Empty;
    bool PrivateAccess=>User.HasClaim(x=>(x.Type is "permission" or "permissions")&&x.Value=="one_on_one.private_notes.read");
    [HttpGet("")] public async Task<IActionResult>Index(CancellationToken c)=>View(await sessions.Dashboard(O,U,PrivateAccess,c));
    [HttpGet("Sessions")] public async Task<IActionResult>Sessions(CancellationToken c)=>View(await sessions.List(O,c));
    [HttpGet("Sessions/Create")] public IActionResult Create()=>View();
    [ValidateAntiForgeryToken,HttpPost("Sessions/Create")] public async Task<IActionResult>Create(CreateOneOnOneSessionRequest request,CancellationToken c){if(!ModelState.IsValid)return View(request);try{var id=await create.Execute(O,U,request,c);return RedirectToAction(nameof(Details),new{id});}catch(ArgumentException e){ModelState.AddModelError("",e.Message);return View(request);}}
    [HttpGet("Sessions/Details/{id:guid}")] public async Task<IActionResult>Details(Guid id,CancellationToken c){var x=await sessions.Get(O,id,U,PrivateAccess,c);return x is null?NotFound():View(x);}
    [ValidateAntiForgeryToken,HttpPost("Sessions/{id:guid}/Agenda")]public async Task<IActionResult>GenerateAgenda(Guid id,string? evidence,CancellationToken c){await agenda.Execute(O,id,evidence??"Evidências registradas na sessão",c);return RedirectToAction(nameof(Details),new{id});}
    [ValidateAntiForgeryToken,HttpPost("Sessions/{id:guid}/Feedback")]public async Task<IActionResult>AddFeedback(Guid id,Guid toUserId,string feedbackText,string evidence,CancellationToken c){await feedback.Execute(O,U,id,toUserId,feedbackText,evidence,c);return RedirectToAction(nameof(Details),new{id});}
    [ValidateAntiForgeryToken,HttpPost("Sessions/{id:guid}/PrivateNote")]public async Task<IActionResult>AddPrivateNote(Guid id,string note,CancellationToken c){await repository.AddPrivateNote(O,U,id,note,c);return RedirectToAction(nameof(Details),new{id});}
    [ValidateAntiForgeryToken,HttpPost("Sessions/{id:guid}/Commitment")]public async Task<IActionResult>AddCommitment(Guid id,Guid responsibleUserId,string title,string? description,DateTime dueAt,bool createAction,CancellationToken c){await commitments.Execute(O,U,id,responsibleUserId,title,description,dueAt,createAction,c);return RedirectToAction(nameof(Details),new{id});}
    [ValidateAntiForgeryToken,HttpPost("Sessions/{id:guid}/Complete")]public async Task<IActionResult>Complete(Guid id,CompleteOneOnOneSessionRequest request,CancellationToken c){await complete.Execute(O,id,U,request,c);return RedirectToAction(nameof(Details),new{id});}
}
