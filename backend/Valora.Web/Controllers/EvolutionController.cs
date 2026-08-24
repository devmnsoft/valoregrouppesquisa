using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[Authorize]
[Route("Evolution")]
public sealed class EvolutionController : Controller
{
    [HttpGet("")] public IActionResult Index() => View();
}
