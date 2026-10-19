using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common;
using Valora.Application.Contracts;
using Valora.Web.Models;
using Valora.Web.Models.ViewModels;
using Valora.Web.Services.Bff;

namespace Valora.Web.Controllers;

public sealed class ResultsController : Controller {
    private readonly ILogger<ResultsController> _logger;
    private readonly IBffApiClient? _api;
    private readonly PublicAccessSessionStore? _publicSessions;
    private readonly ICurrentRequestContext? _currentRequest;
    private readonly IResponseRepository? _responseRepo;
    private readonly IResultRepository? _resultRepo;
    private readonly ISurveyRepository? _surveyRepo;

    [ActivatorUtilitiesConstructor]
    public ResultsController(
        ILogger<ResultsController> logger,
        IBffApiClient api,
        PublicAccessSessionStore publicSessions,
        ICurrentRequestContext currentRequest,
        IResponseRepository responseRepo,
        IResultRepository resultRepo,
        ISurveyRepository surveyRepo) {
        _logger = logger;
        _api = api;
        _publicSessions = publicSessions;
        _currentRequest = currentRequest;
        _responseRepo = responseRepo;
        _resultRepo = resultRepo;
        _surveyRepo = surveyRepo;
    }

    // Overload de compatibilidade para suítes de testes unitários anteriores
    public ResultsController(
        ILogger<ResultsController> logger,
        IBffApiClient api,
        PublicAccessSessionStore publicSessions) {
        _logger = logger;
        _api = api;
        _publicSessions = publicSessions;
    }

    private Guid? OrganizationId => _currentRequest?.GetCurrent().EffectiveOrganizationId;

    [HttpGet("/Results")]
    public async Task<IActionResult> Index([FromQuery] string? search, [FromQuery] string? maturity) {
        ViewData["Title"] = "Resultados";

        if (_responseRepo is null || _resultRepo is null) {
            return View(new ResultsIndexViewModel([], 0, 0, 0, 0, search, maturity));
        }

        if (OrganizationId is not { } organizationId) {
            return View("NoOrganization");
        }

        try {
            var rawResponses = await _responseRepo.ListAdminAsync(organizationId);
            var items = new List<ResultItemViewModel>();

            foreach (var item in rawResponses) {
                var dict = (IDictionary<string, object?>)item;

                Guid responseId = dict.TryGetValue("id", out var idVal) && idVal is Guid gId ? gId : Guid.Empty;
                if (responseId == Guid.Empty) continue;

                string status = dict.TryGetValue("status", out var stVal) && stVal is not null ? stVal.ToString()! : "";
                string surveyTitle = dict.TryGetValue("survey_title", out var sVal) && sVal is not null ? sVal.ToString()! : "Diagnóstico Geral";
                Guid? surveyId = dict.TryGetValue("survey_id", out var survIdVal) && survIdVal is Guid gSurv ? gSurv : null;

                string rawName = dict.TryGetValue("participant_name", out var pNameVal) && pNameVal is not null ? pNameVal.ToString()! : "";
                string rawEmail = dict.TryGetValue("participant_email", out var pEmailVal) && pEmailVal is not null ? pEmailVal.ToString()! : "";

                DateTimeOffset completedAt = DateTimeOffset.UtcNow;
                if (dict.TryGetValue("completed_at", out var cdtVal) && cdtVal is DateTime cdt) {
                    completedAt = new DateTimeOffset(cdt, TimeSpan.Zero);
                } else if (dict.TryGetValue("created_at", out var dtVal) && dtVal is DateTime dt) {
                    completedAt = new DateTimeOffset(dt, TimeSpan.Zero);
                }

                // Buscar resultado calculado oficial
                var score = await _resultRepo.GetByResponseAsync(responseId);

                string maturityLabel = score?.MaturityLabel ?? (string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase) ? "Em Processamento" : "Pendente");
                decimal? total = score?.TotalScore;
                decimal? max = score?.MaxScore;
                decimal? percentage = score?.Percentage;
                string? radar = score?.RadarText;

                bool hasSufficiency = score is not null && score.MaxScore > 0;

                var itemVm = new ResultItemViewModel(
                    responseId,
                    surveyId,
                    surveyTitle,
                    MaskName(rawName),
                    MaskEmail(rawEmail),
                    total,
                    max,
                    percentage,
                    maturityLabel,
                    radar,
                    completedAt,
                    hasSufficiency
                );

                if (!string.IsNullOrWhiteSpace(search)) {
                    var s = search.Trim();
                    if (!itemVm.SurveyTitle.Contains(s, StringComparison.OrdinalIgnoreCase) &&
                        !itemVm.ParticipantMasked.Contains(s, StringComparison.OrdinalIgnoreCase) &&
                        !itemVm.ParticipantEmailMasked.Contains(s, StringComparison.OrdinalIgnoreCase) &&
                        !itemVm.MaturityLabel.Contains(s, StringComparison.OrdinalIgnoreCase)) {
                        continue;
                    }
                }

                if (!string.IsNullOrWhiteSpace(maturity) && !string.Equals(maturity, "all", StringComparison.OrdinalIgnoreCase)) {
                    if (!string.Equals(itemVm.MaturityLabel, maturity, StringComparison.OrdinalIgnoreCase)) {
                        continue;
                    }
                }

                items.Add(itemVm);
            }

            int totalCount = items.Count;
            decimal avg = totalCount > 0 && items.Any(i => i.Percentage.HasValue)
                ? Math.Round(items.Where(i => i.Percentage.HasValue).Average(i => i.Percentage!.Value), 1)
                : 0m;
            int sufficientCount = items.Count(i => i.HasSufficiency);
            int readyCount = items.Count(i => i.Percentage.HasValue && i.HasSufficiency);

            return View(new ResultsIndexViewModel(items, totalCount, avg, sufficientCount, readyCount, search, maturity));
        } catch (Exception ex) {
            _logger.LogError(ex, "Falha ao carregar lista de resultados consolidados.");
            return View(new ResultsIndexViewModel([], 0, 0, 0, 0, search, maturity));
        }
    }

    [AllowAnonymous]
    [Route("public/results/{responseId:guid}")]
    [Route("public/results/{responseId:guid}/executive")]
    [Route("public/results/{responseId:guid}/report")]
    [Route("resultado/{responseId:guid}")]
    public async Task<IActionResult> Public(Guid responseId, string? token, CancellationToken cancellationToken) {
        try {
            if (!string.IsNullOrWhiteSpace(token)) {
                if (_api is null || _publicSessions is null) return StatusCode(503);

                using var validation = await _api.SendAsync(HttpMethod.Get,
                    $"/public/results/{responseId}?token={Uri.EscapeDataString(token)}", null, string.Empty,
                    HttpContext.TraceIdentifier, cancellationToken);
                if (!validation.IsSuccessStatusCode) return StatusCode((int)validation.StatusCode);
                await _publicSessions.WriteAsync(HttpContext, "result", responseId, token, cancellationToken);
                return Redirect($"/public/results/{responseId}");
            }
            ViewData["Title"] = "Public";
            var model = new PublicResultExperienceViewModel { ResponseId = responseId };
            if (!TryValidateModel(model)) return NotFound();
            return View(model);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Falha ao renderizar ResultsController.Public no Valora.Web.");
            throw;
        }
    }

    [AllowAnonymous]
    [Route("resultado/{responseId}/email")]
    public IActionResult Email(string responseId) {
        try {
            if (!Guid.TryParse(responseId, out var parsedResponseId)) return NotFound();
            ViewData["Title"] = "Enviar resultado por e-mail";
            return View("Public", new PublicResultExperienceViewModel { ResponseId = parsedResponseId });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Falha ao renderizar ResultsController.Email no Valora.Web.");
            throw;
        }
    }

    public IActionResult Details(string id) {
        var model = new ResultDetailsViewModel { ResponseId = id };
        if (!TryValidateModel(model)) return NotFound();

        ViewData["Title"] = "Detalhes do resultado";
        return View(model);
    }

    private static string MaskEmail(string? email) {
        if (string.IsNullOrWhiteSpace(email)) return "—";
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1) return "***" + (atIndex >= 0 ? email[atIndex..] : "");
        return string.Concat(email.AsSpan(0, 1), "***", email.AsSpan(atIndex));
    }

    private static string MaskName(string? name) {
        if (string.IsNullOrWhiteSpace(name)) return "Participante Anônimo";
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 1) return parts[0];
        return $"{parts[0]} {parts[^1][0]}.";
    }
}
