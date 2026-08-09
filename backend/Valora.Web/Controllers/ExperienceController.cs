using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class ExperienceController : Controller
{
    [Authorize(Roles = "admin_valora,consultor_valora,empresa_admin,gestor_pesquisa,analista_resultados,gestor_area")]
    public IActionResult Cockpit() => View();
    [Authorize(Roles = "admin_valora,consultor_valora,empresa_admin,gestor_pesquisa")]
    public IActionResult Templates() => View();
    [Authorize(Roles = "admin_valora,consultor_valora,empresa_admin,gestor_pesquisa")]
    public IActionResult Campaigns() => View();
    [Authorize(Roles = "admin_valora,consultor_valora,empresa_admin,gestor_pesquisa,analista_resultados,gestor_area")]
    public IActionResult Help() => View();
}
