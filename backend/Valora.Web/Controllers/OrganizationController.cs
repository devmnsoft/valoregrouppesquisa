using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class OrganizationController : Controller
{
    public IActionResult Index() => View();
}
