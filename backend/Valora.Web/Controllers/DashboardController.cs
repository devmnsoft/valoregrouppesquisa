using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class DashboardController : Controller
{
    public IActionResult Index() => View();
}
