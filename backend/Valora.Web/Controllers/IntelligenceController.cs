using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Models.ViewModels;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class IntelligenceController : Controller
{
    [HttpGet("Intelligence")]
    [HttpGet("InteligenciaOrganizacional")]
    public IActionResult Index() => View();

    [HttpGet("Intelligence/{module}")]
    public IActionResult Module(string module)
    {
        var definition = IntelligenceModuleViewModel.Find(module);
        return definition is null ? NotFound() : View("Module", definition);
    }
}
