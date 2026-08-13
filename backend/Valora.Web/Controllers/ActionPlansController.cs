using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class ActionPlansController : Controller
{
    [HttpGet("ActionPlans")]
    [HttpGet("ValoraAction")]
    public IActionResult Index() => View();
}
