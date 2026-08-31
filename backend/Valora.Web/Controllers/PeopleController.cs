using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common;
using Valora.Application.People;
using Valora.Web.Models.ViewModels;
namespace Valora.Web.Controllers;
[Authorize]
public sealed class PeopleController(ICurrentOrganizationProvider current,PeopleInsightService insights,PeopleProfileService profiles,PeopleTeamService teams):Controller
{
 private Guid? Scope(){var x=current.GetCurrent();return x.IsResolved&&x.OrganizationId!=Guid.Empty?x.OrganizationId:null;} private Guid UserId=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:Guid.Empty;
 private async Task<IActionResult> Page(string section,CancellationToken ct){if(Scope() is not{}o)return View("NoOrganization");return View(section,new PeopleViewModel(await insights.Get(o,ct),await profiles.List(o,ct),await teams.List(o,ct),section));}
 [HttpGet] public Task<IActionResult> Index(CancellationToken ct)=>Page("Index",ct); [HttpGet] public Task<IActionResult> Teams(CancellationToken ct)=>Page("Teams",ct); [HttpGet] public Task<IActionResult> Culture(CancellationToken ct)=>Page("Culture",ct); [HttpGet] public Task<IActionResult> Engagement(CancellationToken ct)=>Page("Engagement",ct); [HttpGet] public Task<IActionResult> Competencies(CancellationToken ct)=>Page("Competencies",ct); [HttpGet] public Task<IActionResult> DevelopmentPlans(CancellationToken ct)=>Page("DevelopmentPlans",ct); [HttpGet] public Task<IActionResult> Risks(CancellationToken ct)=>Page("Risks",ct);
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> CreateProfile(PeopleProfileForm form,CancellationToken ct){if(!ModelState.IsValid){TempData["PeopleError"]="Revise os campos obrigatórios do perfil.";return RedirectToAction(nameof(Index));}if(Scope() is not{}o)return View("NoOrganization");await profiles.Create(o,UserId,new(){DisplayName=form.DisplayName,RoleTitle=form.RoleTitle},ct);TempData["PeopleMessage"]="Perfil criado com escopo organizacional protegido.";return RedirectToAction(nameof(Index));}
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> CreateTeam(PeopleTeamForm form,CancellationToken ct){if(!ModelState.IsValid){TempData["PeopleError"]="Informe o nome e selecione uma liderança válida.";return RedirectToAction(nameof(Teams));}if(Scope() is not{}o)return View("NoOrganization");await teams.Create(o,UserId,new(){Name=form.Name,ResponsibleProfileId=form.ResponsibleProfileId},ct);TempData["PeopleMessage"]="Time criado com vínculo válido.";return RedirectToAction(nameof(Teams));}
}
