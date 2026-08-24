using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[Authorize]
[Route("OneOnOne")]
public sealed class OneOnOneController : Controller
{
    [HttpGet("")] public IActionResult Index() => View();
    [HttpGet("Sessions")] public IActionResult Sessions() => View();
    [HttpGet("Create")] public IActionResult Create() => View();
    [HttpGet("Details/{id:guid}")] public IActionResult Details(Guid id) { ViewData["SessionId"] = id; return View(); }
}
