using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class PlansController : Controller
{
    public IActionResult Index() => View();
}
