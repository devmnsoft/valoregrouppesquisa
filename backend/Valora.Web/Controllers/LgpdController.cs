using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class LgpdController : Controller
{
    [Route("lgpd")]
    [Route("privacy")]
    [Route("Privacy")]
    public IActionResult Index() => View();

    [Route("lgpd/solicitacao")]
    public IActionResult Requests() => View();
}
