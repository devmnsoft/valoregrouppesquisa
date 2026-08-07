using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[Authorize(Roles="admin_valora")]
public sealed class EnterpriseController : Controller
{
    [HttpGet("AdminValora")]
    public IActionResult Index(string? module=null)
    { ViewData["Title"]="Admin Valora"; ViewData["Module"]=module??"overview"; return View(); }
}
