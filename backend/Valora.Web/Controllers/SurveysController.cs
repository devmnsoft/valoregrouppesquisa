using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class SurveysController : Controller
{
    public IActionResult Index() => View();
    public IActionResult PublicLinks() => View();
}
