using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Access;
using Valora.Application.ModularSaas;
using Valora.Application.SaasAdministration;
using Valora.Web.Models.ViewModels;

namespace Valora.Web.Controllers;

[Authorize(Roles = "admin_valora")]
[Route("Admin")]
public sealed class SaasAdminController(
    SaasCustomerService customers,
    CommercialSaasService saas,
    ILogger<SaasAdminController> logger) : Controller
{
    [HttpGet("SaaS")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(new SaasAdminDashboardViewModel(
        await customers.ListAsync(cancellationToken),
        await saas.ListModulesAsync(null, cancellationToken),
        await saas.ListPlansAsync(cancellationToken)));

    [HttpGet("Clients")]
    public async Task<IActionResult> Customers(CancellationToken cancellationToken) =>
        View(await customers.ListAsync(cancellationToken));

    [HttpGet("Clients/Create")]
    public IActionResult CreateClient() => RedirectToAction("CreateOrganization", "AdminHub");

    [HttpGet("Clients/{id:guid}")]
    public IActionResult Client(Guid id) => RedirectToAction(nameof(ClientModules), new { id });

    [HttpGet("Clients/{id:guid}/Modules")]
    public async Task<IActionResult> ClientModules(Guid id, CancellationToken cancellationToken)
    {
        var client = await customers.GetAsync(id, cancellationToken);
        if (client is null) return NotFound();
        return View(new SaasClientModulesViewModel(client,
            await saas.ListModulesAsync(client.OrganizationId, cancellationToken)));
    }

    [HttpGet("Clients/{id:guid}/Users")]
    public IActionResult ClientUsers(Guid id) => View("ClientArea", new SaasClientAreaViewModel(id, "Usuários", "Gerencie usuários, status e perfis do cliente pelo fluxo RBAC."));

    [HttpGet("Clients/{id:guid}/Billing")]
    public IActionResult Billing(Guid id) => View("ClientArea", new SaasClientAreaViewModel(id, "Assinatura e cobrança", "Consulte plano, limites, módulos e histórico comercial."));

    [HttpGet("Billing")]
    public IActionResult Billing() => RedirectToAction(nameof(Customers));

    [HttpGet("Clients/{id:guid}/Audit")]
    public IActionResult Audit(Guid id) => View("ClientArea", new SaasClientAreaViewModel(id, "Auditoria do cliente", "Alterações de plano, módulos, permissões e acesso ficam registradas."));

    [HttpGet("Audit")]
    public IActionResult Audit() => RedirectToAction(nameof(Customers));

    [HttpGet("Modules")]
    public async Task<IActionResult> Modules(CancellationToken cancellationToken) =>
        View("Modules", await saas.ListModulesAsync(null, cancellationToken));

    [HttpGet("Plans")]
    public async Task<IActionResult> Plans(CancellationToken cancellationToken) =>
        View("Plans", await saas.ListPlansAsync(cancellationToken));

    [HttpGet("Permissions")]
    public IActionResult Permissions() => View(ValoraPermissions.All.Order(StringComparer.Ordinal).ToArray());

    [ValidateAntiForgeryToken, HttpPost("Clients/{id:guid}/Modules")]
    public async Task<IActionResult> ChangeModule(Guid id, ChangeClientModuleViewModel model, CancellationToken cancellationToken)
    {
        model.ClientId = id;
        if (!ModelState.IsValid)
        {
            TempData["Warning"] = "Revise o status e informe um motivo com pelo menos 10 caracteres.";
            return RedirectToAction(nameof(ClientModules), new { id });
        }
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) || actorId == Guid.Empty)
            return Forbid();

        try
        {
            await saas.SetModuleStatusAsync(id, model.ModuleCode, model.Status, actorId, model.Reason,
                HttpContext.TraceIdentifier, cancellationToken);
            TempData["Success"] = "Contratação do módulo atualizada com auditoria.";
        }
        catch (Exception exception) when (exception is ValidationException or InvalidOperationException)
        {
            logger.LogWarning(exception,
                "Module contract update rejected. ClientId={ClientId} UserId={UserId} ModuleCode={ModuleCode} CorrelationId={CorrelationId}",
                id, actorId, model.ModuleCode, HttpContext.TraceIdentifier);
            TempData["Warning"] = exception.Message;
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Module contract update failed. ClientId={ClientId} UserId={UserId} ModuleCode={ModuleCode} CorrelationId={CorrelationId}",
                id, actorId, model.ModuleCode, HttpContext.TraceIdentifier);
            TempData["Error"] = "Não foi possível salvar agora. Tente novamente ou acione o suporte.";
        }
        return RedirectToAction(nameof(ClientModules), new { id });
    }
}
