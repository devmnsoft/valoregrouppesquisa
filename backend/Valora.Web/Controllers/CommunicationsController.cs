using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common;
using Valora.Application.Contracts;
using Valora.Web.Models.ViewModels;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class CommunicationsController(
    IEmailOperationalRepository emailRepo,
    ICurrentRequestContext currentRequest,
    IWebHostEnvironment env,
    ILogger<CommunicationsController> logger) : Controller {

    private Guid? OrganizationId => currentRequest.GetCurrent().EffectiveOrganizationId;

    [HttpGet("/Communications")]
    [HttpGet("/Notifications")]
    public async Task<IActionResult> Index([FromQuery] string? status, [FromQuery] string? search) {
        if (OrganizationId is not { } organizationId) return View("NoOrganization");

        try {
            var isDev = env.IsDevelopment() || env.IsEnvironment("Homologation");
            var statusDto = await emailRepo.StatusAsync(organizationId, isDev);
            var jobs = await emailRepo.ListJobsAsync(organizationId, string.IsNullOrWhiteSpace(status) ? null : status.Trim());
            var templates = await emailRepo.ListTemplatesAsync(organizationId);

            if (!string.IsNullOrWhiteSpace(search)) {
                var s = search.Trim();
                jobs = jobs.Where(j =>
                    j.Subject.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    j.ToEmailMasked.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    j.TemplateCode.Contains(s, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return View(new CommunicationsIndexViewModel(statusDto, jobs, templates, status, search));
        }
        catch (Exception ex) {
            logger.LogError(ex, "Falha ao carregar Central de Comunicações para a organização {OrganizationId}", organizationId);
            TempData["ErrorMessage"] = "Não foi possível carregar as informações de comunicações no momento.";
            return View(new CommunicationsIndexViewModel(new(0, 0, 0, 0, 0, false), [], []));
        }
    }

    [HttpPost("/Communications/Retry/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Retry(Guid id) {
        if (OrganizationId is not { } organizationId) return View("NoOrganization");

        try {
            var job = await emailRepo.GetJobAsync(id);
            if (job is null || (job.OrganizationId.HasValue && job.OrganizationId != organizationId)) {
                TempData["ErrorMessage"] = "Disparo não encontrado ou não pertence a esta organização.";
                return RedirectToAction(nameof(Index));
            }

            await emailRepo.MarkProcessingAsync(id);
            TempData["SuccessMessage"] = "Disparo reenfileirado com sucesso para nova tentativa.";
        }
        catch (Exception ex) {
            logger.LogError(ex, "Falha ao reenfileirar e-mail {EmailJobId}", id);
            TempData["ErrorMessage"] = "Ocorreu um erro ao reenfileirar o e-mail.";
        }

        return RedirectToAction(nameof(Index));
    }
}
