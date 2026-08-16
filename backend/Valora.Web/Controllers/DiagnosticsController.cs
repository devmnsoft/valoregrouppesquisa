using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Valora.Web.Controllers;
[Authorize]
public sealed class DiagnosticsController:Controller
{
    [HttpGet("Diagnostics")]
    public IActionResult Index()=>RedirectToAction("Index","Surveys");
    [HttpGet("Diagnostics/New")]
    public IActionResult New()=>RedirectToAction("Index","Forms");
    [HttpGet("Diagnostics/Audiences")]
    public IActionResult Audiences()=>RedirectToAction("PublicLinks","Surveys");
    [HttpGet("Diagnostics/{id:guid}")]
    public IActionResult Details(Guid id)=>RedirectToAction(nameof(Workspace),new{id});
    [HttpGet("Diagnostics/{id:guid}/Workspace")]
    public IActionResult Workspace(Guid id){ViewData["DiagnosticId"]=id;return View();}
}
