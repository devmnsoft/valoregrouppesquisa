using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common;
using Valora.Application.SolutionPacks;
using Valora.Web.Models.ViewModels;
namespace Valora.Web.Controllers;
[Authorize]
public sealed class SolutionPacksController(ICurrentOrganizationProvider current,SolutionPackService packs,SolutionPackInstallationService installations,SolutionPackCatalogService catalog):Controller
{
 private Guid? Organization(){var value=current.GetCurrent();return value.IsResolved&&value.OrganizationId!=Guid.Empty?value.OrganizationId:null;}
 [HttpGet] public async Task<IActionResult> Index(string? segment,string? category,CancellationToken ct){var o=Organization();if(o is null)return View("NoOrganization");return View(new SolutionPackMarketplaceViewModel(await packs.List(o.Value,segment,category,ct),segment,category));}
 [HttpGet("/SolutionPacks/{id:guid}")] public async Task<IActionResult> Details(Guid id,CancellationToken ct){var o=Organization();if(o is null)return View("NoOrganization");var model=await packs.Get(id,o.Value,ct);return model is null?NotFound():View(model);}
 [HttpGet] public IActionResult Builder()=>View(new CreateSolutionPackRequest("","","","",false,null));
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Builder(CreateSolutionPackRequest request,CancellationToken ct){if(!ModelState.IsValid)return View(request);var o=Organization();if(o is null)return View("NoOrganization");if(!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var actor)||actor==Guid.Empty){ModelState.AddModelError("","Sua sessão não possui usuário válido.");return View(request);}var id=await packs.Create(o.Value,actor,request,ct);TempData["Success"]="Pacote criado como rascunho. Adicione uma versão antes de publicar.";return RedirectToAction(nameof(Details),new{id});}
 [HttpGet] public async Task<IActionResult> Installations(CancellationToken ct){var o=Organization();if(o is null)return View("NoOrganization");return View(new SolutionPackInstallationsViewModel(await installations.List(o.Value,ct),await catalog.Updates(o.Value,ct)));}
 [HttpGet] public async Task<IActionResult> Updates(CancellationToken ct)=>await Installations(ct);
}
