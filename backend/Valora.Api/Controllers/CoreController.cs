using Microsoft.AspNetCore.Mvc;
namespace Valora.Api.Controllers;
[ApiController]
[Route("/legacy/planned")]
public sealed class CoreController : ControllerBase
{
    [HttpGet]
    public IActionResult Planned() => Ok(new { status = "superseded_by_phase_1_postgresql_api", firebase = "preserved" });
}
