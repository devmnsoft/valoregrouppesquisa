using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Error() => View("Index");
}
