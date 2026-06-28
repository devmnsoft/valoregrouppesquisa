using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class CommunicationsController : Controller
{
    public IActionResult Index() => View();
}
