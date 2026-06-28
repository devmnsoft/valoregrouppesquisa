using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class ResultsController : Controller
{
    [Route("r/{responseId}")]
    public IActionResult Public(string responseId) { ViewBag.ResponseId = responseId; return View(); }
    public IActionResult Details(string id) { ViewBag.ResponseId = id; return View(); }
}
