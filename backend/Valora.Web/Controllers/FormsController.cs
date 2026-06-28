using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class FormsController : Controller
{
    public IActionResult Index() => View();
}
