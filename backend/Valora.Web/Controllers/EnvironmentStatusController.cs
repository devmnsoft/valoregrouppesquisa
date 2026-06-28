using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class EnvironmentStatusController : Controller
{
    public IActionResult Index() => View();
}
