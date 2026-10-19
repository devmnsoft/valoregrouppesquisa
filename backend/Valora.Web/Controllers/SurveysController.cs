using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common;
using Valora.Application.Contracts;
using Valora.Web.Models.ViewModels;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class SurveysController : Controller {
    private readonly ISurveyRepository? _surveyRepo;
    private readonly ICurrentRequestContext? _currentRequest;
    private readonly ILogger<SurveysController> _logger;

    [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
    public SurveysController(
        ISurveyRepository surveyRepo,
        ICurrentRequestContext currentRequest,
        ILogger<SurveysController> logger) {
        _surveyRepo = surveyRepo;
        _currentRequest = currentRequest;
        _logger = logger;
    }

    // Backward-compatible constructor for isolated unit tests
    public SurveysController(ILogger<SurveysController> logger) {
        _logger = logger;
    }

    private Guid? OrganizationId => _currentRequest?.GetCurrent().EffectiveOrganizationId;

    [HttpGet("/Surveys")]
    public IActionResult Index() {
        try {
            ViewData["Title"] = "Diagnósticos";
            return View();
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Falha ao renderizar SurveysController.Index no Valora.Web.");
            throw;
        }
    }

    [HttpGet("/Surveys/PublicLinks")]
    public async Task<IActionResult> PublicLinks([FromQuery] Guid? surveyId) {
        if (_surveyRepo is null) {
            ViewData["Title"] = "Links públicos";
            return View(new SurveysPublicLinksViewModel([], null, null, null, [], 0, 0, 0, 0));
        }

        if (OrganizationId is not { } organizationId) {
            return View("NoOrganization");
        }

        try {
            ViewData["Title"] = "Links públicos";
            var allSurveys = await _surveyRepo.ListAdminAsync(organizationId);

            var surveyOptions = allSurveys.Select(s => new SurveyOptionItem(
                AsGuid(GetProp(s, "id")),
                AsString(GetProp(s, "title") ?? GetProp(s, "name"), "Pesquisa sem título"),
                AsString(GetProp(s, "status"), "draft"),
                GetProp(s, "form_name")?.ToString(),
                AsInt(GetProp(s, "completed_responses"))
            )).ToList();

            SurveyOptionItem? selectedSurvey = null;
            if (surveyId.HasValue) {
                selectedSurvey = surveyOptions.FirstOrDefault(s => s.Id == surveyId.Value);
            }
            selectedSurvey ??= surveyOptions.FirstOrDefault(s => s.Status is "active" or "published")
                              ?? surveyOptions.FirstOrDefault();

            var links = new List<PublicLinkItemViewModel>();
            if (selectedSurvey is not null) {
                var rawLinks = await _surveyRepo.ListLinksAdminAsync(organizationId, selectedSurvey.Id);
                links = rawLinks.Select(l => new PublicLinkItemViewModel(
                    AsGuid(GetProp(l, "id")),
                    AsGuid(GetProp(l, "survey_id")),
                    AsString(GetProp(l, "public_url")),
                    AsString(GetProp(l, "status"), "active"),
                    AsDateTimeOffset(GetProp(l, "starts_at")),
                    AsDateTimeOffset(GetProp(l, "expires_at")),
                    AsDateTimeOffset(GetProp(l, "created_at")) ?? DateTimeOffset.UtcNow
                )).ToList();
            }

            var totalSurveys = surveyOptions.Count;
            var totalLinks = links.Count;
            var activeLinks = links.Count(l => l.IsActive);
            var expiredOrRevoked = links.Count(l => !l.IsActive);

            var vm = new SurveysPublicLinksViewModel(
                surveyOptions,
                selectedSurvey?.Id,
                selectedSurvey?.Title,
                selectedSurvey?.Status,
                links,
                totalSurveys,
                totalLinks,
                activeLinks,
                expiredOrRevoked);

            return View(vm);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Falha ao renderizar SurveysController.PublicLinks para a organização {OrganizationId}", organizationId);
            if (TempData is not null) {
                TempData["ErrorMessage"] = "Não foi possível carregar os links públicos no momento.";
            }
            return View(new SurveysPublicLinksViewModel([], null, null, null, [], 0, 0, 0, 0));
        }
    }

    [HttpPost("/Surveys/PublicLinks/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLink([FromForm] Guid surveyId, [FromForm] int? expiresInDays) {
        if (_surveyRepo is null) {
            if (TempData is not null) TempData["ErrorMessage"] = "Serviço de pesquisas indisponível.";
            return RedirectToAction(nameof(PublicLinks));
        }

        if (OrganizationId is not { } organizationId) {
            return View("NoOrganization");
        }

        try {
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
            var publicUrl = $"/public/surveys/{surveyId}?token={token}";
            var expiresAt = expiresInDays.HasValue && expiresInDays.Value > 0
                ? DateTimeOffset.UtcNow.AddDays(expiresInDays.Value)
                : (DateTimeOffset?)null;

            await _surveyRepo.CreateLinkAdminAsync(organizationId, surveyId, tokenHash, publicUrl, expiresAt);
            if (TempData is not null) {
                TempData["SuccessMessage"] = "Link público de coleta gerado com sucesso! Você já pode compartilhá-lo.";
            }
        }
        catch (InvalidOperationException ex) {
            _logger.LogWarning(ex, "Pesquisa {SurveyId} não está apta para geração de links na organização {OrganizationId}", surveyId, organizationId);
            if (TempData is not null) TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Erro ao gerar link público para a pesquisa {SurveyId}", surveyId);
            if (TempData is not null) TempData["ErrorMessage"] = "Ocorreu um erro ao gerar o link público.";
        }

        return RedirectToAction(nameof(PublicLinks), new { surveyId });
    }

    [HttpPost("/Surveys/PublicLinks/{linkId:guid}/Status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateLinkStatus(Guid linkId, [FromForm] Guid surveyId, [FromForm] string status) {
        if (_surveyRepo is null) {
            if (TempData is not null) TempData["ErrorMessage"] = "Serviço de pesquisas indisponível.";
            return RedirectToAction(nameof(PublicLinks));
        }

        if (OrganizationId is not { } organizationId) {
            return View("NoOrganization");
        }

        if (status is not ("active" or "inactive" or "revoked")) {
            if (TempData is not null) TempData["ErrorMessage"] = "Situação informada é inválida.";
            return RedirectToAction(nameof(PublicLinks), new { surveyId });
        }

        try {
            await _surveyRepo.UpdateLinkStatusAdminAsync(organizationId, linkId, status);
            if (TempData is not null) {
                TempData["SuccessMessage"] = "A situação do link de coleta foi atualizada.";
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Erro ao atualizar situação do link {LinkId}", linkId);
            if (TempData is not null) TempData["ErrorMessage"] = "Não foi possível atualizar a situação do link.";
        }

        return RedirectToAction(nameof(PublicLinks), new { surveyId });
    }

    private static object? GetProp(object? obj, string propName) {
        if (obj is null) return null;
        if (obj is IDictionary<string, object> dict) {
            return dict.TryGetValue(propName, out var v) ? v : null;
        }
        return obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)?.GetValue(obj);
    }

    private static Guid AsGuid(object? value) => value switch {
        Guid g => g,
        string s when Guid.TryParse(s, out var parsed) => parsed,
        _ => Guid.Empty
    };

    private static string AsString(object? value, string fallback = "") => value?.ToString() ?? fallback;

    private static DateTimeOffset? AsDateTimeOffset(object? value) => value switch {
        DateTimeOffset dto => dto,
        DateTime dt => new DateTimeOffset(dt, TimeSpan.Zero),
        _ => null
    };

    private static int AsInt(object? value) => value switch {
        int i => i,
        long l => (int)l,
        short s => s,
        _ => 0
    };
}
