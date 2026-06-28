using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class AuditController : Controller
{
    public IActionResult Index() => View();
}
