using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class MethodologyController : Controller
{
    [HttpGet("Methodology")]
    [HttpGet("Methodology/Dictionary")]
    [HttpGet("Intelligence/Dictionary")]
    public IActionResult Dictionary() => View();

    [HttpGet("Methodology/CognitiveMap")]
    [HttpGet("Intelligence/CognitiveMap")]
    public IActionResult CognitiveMap() => View();

    [HttpGet("Methodology/Mappings")]
    public IActionResult Mappings() => View();
}
