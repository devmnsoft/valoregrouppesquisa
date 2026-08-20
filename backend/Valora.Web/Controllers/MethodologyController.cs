using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class MethodologyController : Controller
{
    [HttpGet("Methodology/Overview")]
    public IActionResult Overview() => View();

    [HttpGet("Methodology/Dimensions")]
    public IActionResult Dimensions() => View(nameof(Overview));
    [HttpGet("Methodology/Concepts")]
    public IActionResult Concepts() => View(nameof(Overview));
    [HttpGet("Methodology/OfficialQuestions")]
    public IActionResult OfficialQuestions() => View(nameof(Overview));
    [HttpGet("Methodology/Templates")]
    public IActionResult Templates() => View(nameof(Overview));
    [HttpGet("Methodology/Scoring")]
    public IActionResult Scoring() => View(nameof(Overview));
    [HttpGet("Methodology/Recommendations")]
    public IActionResult Recommendations() => View(nameof(Overview));
    [HttpGet("Methodology/Versions")]
    public IActionResult Versions() => View(nameof(Overview));

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
