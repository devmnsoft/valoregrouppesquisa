using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common;
using Valora.Application.Contracts;
using Valora.Web.Models.ViewModels;

namespace Valora.Web.Controllers;

public sealed class CertificatesController : Controller {
    private readonly ICertificateOperationalService? _certService;
    private readonly ICurrentRequestContext? _currentRequest;
    private readonly ILogger<CertificatesController> _logger;

    [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
    public CertificatesController(
        ICertificateOperationalService certService,
        ICurrentRequestContext currentRequest,
        ILogger<CertificatesController> logger) {
        _certService = certService;
        _currentRequest = currentRequest;
        _logger = logger;
    }

    public CertificatesController(ILogger<CertificatesController> logger) {
        _logger = logger;
    }

    private Guid? OrganizationId => _currentRequest?.GetCurrent().EffectiveOrganizationId;

    [HttpGet("/Certificates")]
    public async Task<IActionResult> Index([FromQuery] string? search) {
        if (_certService is null) {
            ViewData["Title"] = "Certificados";
            return View(new CertificatesIndexViewModel([], 0, 0, 0, search));
        }

        if (OrganizationId is not { } organizationId) {
            return View("NoOrganization");
        }

        try {
            ViewData["Title"] = "Certificados";
            var rawCerts = await _certService.ListAsync(organizationId);

            var items = rawCerts.Select(c => new CertificateItemViewModel(
                c.Id,
                c.ResponseId,
                string.IsNullOrWhiteSpace(c.ParticipantName) ? "Participante não identificado" : c.ParticipantName,
                string.IsNullOrWhiteSpace(c.ParticipantEmailMasked) ? "—" : c.ParticipantEmailMasked,
                string.IsNullOrWhiteSpace(c.CompanyName) ? "—" : c.CompanyName,
                c.TotalScore,
                string.IsNullOrWhiteSpace(c.Level) ? "Maturidade não informada" : c.Level,
                c.ValidationCode,
                c.ValidationUrl,
                c.Status,
                c.IssuedAt
            )).ToList();

            if (!string.IsNullOrWhiteSpace(search)) {
                var s = search.Trim();
                items = items.Where(c =>
                    c.ParticipantName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    c.ValidationCode.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    c.CompanyName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    c.Level.Contains(s, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var total = items.Count;
            var issued = items.Count(c => c.IsIssued);
            var revoked = items.Count(c => !c.IsIssued);

            return View(new CertificatesIndexViewModel(items, total, issued, revoked, search));
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Falha ao listar certificados para a organização {OrganizationId}", organizationId);
            if (TempData is not null) TempData["ErrorMessage"] = "Não foi possível carregar os certificados no momento.";
            return View(new CertificatesIndexViewModel([], 0, 0, 0, search));
        }
    }

    [HttpPost("/Certificates/{id:guid}/Revoke")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke(Guid id) {
        if (_certService is null) {
            if (TempData is not null) TempData["ErrorMessage"] = "Serviço de certificados indisponível.";
            return RedirectToAction(nameof(Index));
        }

        if (OrganizationId is not { } organizationId) {
            return View("NoOrganization");
        }

        try {
            await _certService.RevokeAsync(organizationId, id);
            if (TempData is not null) TempData["SuccessMessage"] = "Certificado revogado com sucesso.";
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Erro ao revogar certificado {CertificateId}", id);
            if (TempData is not null) TempData["ErrorMessage"] = "Ocorreu um erro ao revogar o certificado.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/Certificates/{id:guid}/Download")]
    public async Task<IActionResult> Download(Guid id) {
        if (_certService is null) return NotFound();
        if (OrganizationId is not { } organizationId) return View("NoOrganization");

        try {
            var html = await _certService.DownloadHtmlAsync(organizationId, id);
            if (html is null) return NotFound();

            var cert = await _certService.GetAsync(organizationId, id);
            var filename = $"Certificado-Valora-{(cert?.ValidationCode ?? id.ToString("N"))}.html";
            return File(System.Text.Encoding.UTF8.GetBytes(html), "text/html; charset=utf-8", filename);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Erro ao baixar certificado {CertificateId}", id);
            return NotFound();
        }
    }

    [AllowAnonymous]
    [Route("certificado/{certificateId}")]
    [Route("public/results/{certificateId}/certificate")]
    public IActionResult Public(string certificateId) {
        try {
            ViewData["Title"] = "Certificado";
            ViewData["ResponseId"] = certificateId;
            return View("Details");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Falha ao renderizar CertificatesController.Public no Valora.Web.");
            throw;
        }
    }

    public IActionResult Details(string id) {
        try {
            ViewData["Title"] = "Details";
            ViewData["ResponseId"] = id;
            return View();
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Falha ao renderizar CertificatesController.Details no Valora.Web.");
            throw;
        }
    }

    [AllowAnonymous]
    [Route("Certificates/Validate/{certificateCode?}")]
    [Route("certificado/validar/{certificateCode?}")]
    public IActionResult Validate(string? certificateCode) {
        try {
            ViewData["Title"] = "Validate";
            ViewData["CertificateCode"] = certificateCode;
            return View();
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Falha ao renderizar CertificatesController.Validate no Valora.Web.");
            throw;
        }
    }
}
