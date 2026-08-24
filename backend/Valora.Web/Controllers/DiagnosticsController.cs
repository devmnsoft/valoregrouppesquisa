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
    [HttpGet("Diagnostics/Create")]
    public IActionResult Create()=>RedirectToAction(nameof(New));
    [HttpGet("Diagnostics/Audiences")]
    public IActionResult Audiences()=>RedirectToAction("PublicLinks","Surveys");
    [HttpGet("Diagnostics/{id:guid}")]
    public IActionResult Details(Guid id)=>RedirectToAction(nameof(Workspace),new{id});
    [HttpGet("Diagnostics/{id:guid}/Workspace")]
    public IActionResult Workspace(Guid id){ViewData["DiagnosticId"]=id;return View();}
    [HttpGet("Diagnostics/Details/{id:guid}")]
    public IActionResult DetailsCanonical(Guid id)=>RedirectToAction(nameof(Workspace),new{id});
    [HttpGet("Diagnostics/Collect/{id:guid}")]
    public IActionResult Collect(Guid id)=>RedirectToAction("PublicLinks","Surveys",new{surveyId=id});
    [HttpGet("Diagnostics/Results/{id:guid}")]
    public IActionResult Results(Guid id)=>RedirectToAction("Details","Results",new{id});
}
