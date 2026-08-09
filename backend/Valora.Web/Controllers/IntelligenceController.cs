using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class IntelligenceController : Controller
{
    [HttpGet("InteligenciaOrganizacional")]
    public IActionResult Index() => View();
}
