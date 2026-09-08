using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.ModularSaas;
using Valora.Web.Models.ViewModels;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class SaasController(CommercialSaasService saas, ILogger<SaasController> logger) : Controller
{
    [HttpGet("/Modules")]
    [HttpGet("/Marketplace")]
    [HttpGet("/Subscription")]
    [HttpGet("/Subscription/Modules")]
    public Task<IActionResult> Marketplace(string? blocked, CancellationToken cancellationToken) =>
        RenderMarketplaceAsync(blocked, cancellationToken);

    [HttpGet("/Plans")]
    [HttpGet("/Organization/Upgrade")]
    [HttpGet("/Platform/Plans")]
    public Task<IActionResult> Plans(CancellationToken cancellationToken) => RenderMarketplaceAsync(null, cancellationToken);

    [HttpGet("/Subscription/Usage")]
    [HttpGet("/Organization/Consumption")]
    [HttpGet("/Platform/Usage")]
    public IActionResult Usage() => LegacyPage("Consumo", "Uso mensal, limites e preservação dos dados.", "usage");

    [HttpGet("/Organization/MyPlan")]
    [HttpGet("/Platform/Subscriptions")]
    public IActionResult Subscription() => LegacyPage("Minha assinatura", "Plano, módulos contratados e limites vigentes.", "my-plan");

    [HttpGet("/Platform/Invoices")]
    public IActionResult Invoices() => LegacyPage("Faturas", "Cobrança e histórico financeiro.", "invoices");

    [ValidateAntiForgeryToken, HttpPost("/Subscription/Modules/Request")]
    public async Task<IActionResult> RequestUpgrade(RequestModuleUpgradeViewModel model, CancellationToken cancellationToken)
    {
        if (!TryContext(out var clientId, out var userId))
        {
            TempData["Warning"] = "Selecione um cliente para operar esta área.";
            return RedirectToAction(nameof(Marketplace));
        }
        if (!ModelState.IsValid)
        {
            TempData["Warning"] = "Revise o módulo e a justificativa antes de continuar.";
            return RedirectToAction(nameof(Marketplace));
        }

        try
        {
            await saas.RequestUpgradeAsync(clientId, userId, model.ModuleCode, model.Reason,
                HttpContext.TraceIdentifier, cancellationToken);
            TempData["Success"] = "Solicitação de upgrade registrada. Nosso time entrará em contato.";
        }
        catch (Exception exception) when (exception is ValidationException or InvalidOperationException)
        {
            logger.LogWarning(exception,
                "Upgrade request rejected. ClientId={ClientId} UserId={UserId} ModuleCode={ModuleCode} CorrelationId={CorrelationId}",
                clientId, userId, model.ModuleCode, HttpContext.TraceIdentifier);
            TempData["Warning"] = exception.Message;
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Upgrade request failed. ClientId={ClientId} UserId={UserId} ModuleCode={ModuleCode} CorrelationId={CorrelationId}",
                clientId, userId, model.ModuleCode, HttpContext.TraceIdentifier);
            TempData["Error"] = "Não foi possível salvar agora. Tente novamente ou acione o suporte.";
        }
        return RedirectToAction(nameof(Marketplace));
    }

    private async Task<IActionResult> RenderMarketplaceAsync(string? blocked, CancellationToken cancellationToken)
    {
        var isPlatformAdministrator = User.IsInRole("admin_valora");
        var clientId = OrganizationId();
        var modules = await saas.ListModulesAsync(isPlatformAdministrator ? null : clientId, cancellationToken);
        var plans = await saas.ListPlansAsync(cancellationToken);
        ViewData["Title"] = "Módulos e planos";
        return View("Marketplace", new SaasMarketplaceViewModel(modules, plans, isPlatformAdministrator, clientId, blocked));
    }

    private IActionResult LegacyPage(string title, string subtitle, string mode)
    {
        ViewData["Title"] = title;
        ViewData["Subtitle"] = subtitle;
        ViewData["Mode"] = mode;
        return View("Dashboard");
    }

    private Guid? OrganizationId() => Guid.TryParse(User.FindFirstValue("organization_id"), out var id) && id != Guid.Empty ? id : null;

    private bool TryContext(out Guid clientId, out Guid userId)
    {
        clientId = OrganizationId() ?? Guid.Empty;
        userId = Guid.Empty;
        return clientId != Guid.Empty
            && Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId)
            && userId != Guid.Empty;
    }
}
