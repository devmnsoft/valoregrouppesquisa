using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[Authorize]
[Route("Leadership")]
public sealed class LeadershipController : Controller
{
    [HttpGet("")] public IActionResult Index() => View();
    [HttpGet("Details/{id:guid}")] public IActionResult Details(Guid id) { ViewData["LeaderId"] = id; return View(); }
}
