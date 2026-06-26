using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
namespace Valora.Api.Controllers;
[ApiController]
public sealed class CertificatesController(ISurveyRepository surveys) : ControllerBase
{
    [HttpGet("/responses/{responseId:guid}/certificate.pdf")]
    public async Task<IActionResult> Pdf(Guid responseId)
    {
        var result = await surveys.GetResultAsync(responseId);
        if (result is null) return NotFound(new { ok = false, message = "Resultado não encontrado." });
        var bytes = System.Text.Encoding.UTF8.GetBytes($"Valora Pulse™\nCertificado\nResposta: {responseId}\nPontuação: {result.TotalScore}/125\nNível: {result.Level}");
        return File(bytes, "application/pdf", $"certificado-{responseId:N}.pdf");
    }

    [HttpGet("/responses/{responseId:guid}/certificate.png")]
    public async Task<IActionResult> Png(Guid responseId)
    {
        var result = await surveys.GetResultAsync(responseId);
        if (result is null) return NotFound(new { ok = false, message = "Resultado não encontrado." });
        return Ok(new { ok = true, responseId, productName = "Valora Pulse™", score = result.TotalScore, level = result.Level, validationCode = $"VAL-{responseId:N}" });
    }
}
