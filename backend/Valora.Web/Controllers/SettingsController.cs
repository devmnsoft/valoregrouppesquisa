using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class SettingsController : Controller
{
    public IActionResult Index() => View();
}
