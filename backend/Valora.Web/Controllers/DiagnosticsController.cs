using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Valora.Web.Controllers;
[Authorize]
public sealed class DiagnosticsController:Controller
{
    [HttpGet("Diagnostics/{id:guid}/Workspace")]
    public IActionResult Workspace(Guid id){ViewData["DiagnosticId"]=id;return View();}
}
