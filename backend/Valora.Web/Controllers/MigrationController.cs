using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class MigrationController : Controller
{
    public IActionResult Index() => View();
}
