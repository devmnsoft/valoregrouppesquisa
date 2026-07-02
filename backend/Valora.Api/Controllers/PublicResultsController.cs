using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class PublicResultsController(IPublicResultService service, ICertificateService certificates, ICommunicationRepository communications) : ControllerBase
{
    [HttpGet("/public/results/{responseId:guid}")]
    public async Task<IActionResult> GetResult(Guid responseId, [FromQuery] string token)
    {
        var result = await service.GetAsync(responseId, new PublicResultRequest(token));
        return Ok(result);
    }

    [HttpPost("/public/results/{responseId:guid}")]
    public async Task<IActionResult> Result(Guid responseId, PublicResultRequest request)
    {
        var result = await service.GetAsync(responseId, request);
        return Ok(result);
    }

    [HttpGet("/public/results/{responseId:guid}/certificate")]
    public async Task<IActionResult> Certificate(Guid responseId, [FromQuery] string token)
        => Content(await certificates.BuildCertificateHtmlAsync(responseId, token), "text/html; charset=utf-8");

    [HttpGet("/public/results/{responseId:guid}/certificate.pdf")]
    public async Task<IActionResult> CertificatePdf(Guid responseId, [FromQuery] string token)
        => File(await certificates.RenderPdfAsync(responseId, token), "application/pdf", $"certificado-valora-{responseId:N}.pdf");

    [HttpGet("/public/results/{responseId:guid}/certificate.png")]
    public async Task<IActionResult> CertificatePng(Guid responseId, [FromQuery] string token)
        => File(await certificates.RenderImageAsync(responseId, token), "image/png", $"certificado-valora-{responseId:N}.png");

    [HttpPost("/public/results/{responseId:guid}/email")]
    public async Task<IActionResult> ResendEmail(Guid responseId, [FromQuery] string token, [FromBody] Dictionary<string,string>? body)
    {
        var result = await service.GetAsync(responseId, new PublicResultRequest(token));
        var to = body?.GetValueOrDefault("toEmail") ?? result.Response.ParticipantEmail;
        if (string.IsNullOrWhiteSpace(to)) return BadRequest(new { ok = false, code = "EMAIL_REQUIRED" });
        var jobId = await communications.AddEmailJobAsync(result.Company.Id, responseId, to, "Seu resultado do diagnóstico Valora Group está pronto", "result-ready", "pending", System.Text.Json.JsonSerializer.Serialize(new { responseId, result.Survey.Title, result.Result.Percentage, result.Result.MaturityLabel, resultUrl = $"/public/results/{responseId}?token={token}", whatsapp = "https://wa.me/5591992545353" }));
        return Ok(new { ok = true, emailStatus = "pending", emailJobId = jobId });
    }
}
