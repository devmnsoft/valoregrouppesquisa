using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Models;
using Valora.Web.Services.Bff;

namespace Valora.Web.Controllers;

public sealed class ResultsController(ILogger<ResultsController> logger, IBffApiClient api,
    PublicAccessSessionStore publicSessions) : Controller {
    public IActionResult Index() {
        ViewData["Title"] = "Resultados";
        return View();
    }

    [AllowAnonymous]
    [Route("public/results/{responseId:guid}")]
    [Route("public/results/{responseId:guid}/executive")]
    [Route("public/results/{responseId:guid}/report")]
    [Route("resultado/{responseId:guid}")]
    public async Task<IActionResult> Public(Guid responseId, string? token, CancellationToken cancellationToken) {
        try {
            if (!string.IsNullOrWhiteSpace(token)) {
                using var validation = await api.SendAsync(HttpMethod.Get,
                    $"/public/results/{responseId}?token={Uri.EscapeDataString(token)}", null, string.Empty,
                    HttpContext.TraceIdentifier, cancellationToken);
                if (!validation.IsSuccessStatusCode) return StatusCode((int)validation.StatusCode);
                await publicSessions.WriteAsync(HttpContext, "result", responseId, token, cancellationToken);
                return Redirect($"/public/results/{responseId}");
            }
            ViewData["Title"] = "Public";
            var model = new PublicResultExperienceViewModel { ResponseId = responseId };
            if (!TryValidateModel(model)) return NotFound();
            return View(model);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Falha ao renderizar ResultsController.Public no Valora.Web.");
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
            logger.LogError(ex, "Falha ao renderizar ResultsController.Email no Valora.Web.");
            throw;
        }
    }

    public IActionResult Details(string id) {
        var model = new ResultDetailsViewModel { ResponseId = id };
        if (!TryValidateModel(model)) return NotFound();

        ViewData["Title"] = "Detalhes do resultado";
        return View(model);
    }
}
