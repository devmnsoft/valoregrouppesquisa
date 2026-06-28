using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class UsersController : Controller
{
    public IActionResult Index() => View();
}
