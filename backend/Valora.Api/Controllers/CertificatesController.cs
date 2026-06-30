using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.Security;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class CertificatesController(IResponseRepository responses, ICertificateRepository certificates, ILogger<CertificatesController> logger) : ControllerBase
{
    [HttpGet("/responses/{responseId:guid}/certificate.pdf")]
    public async Task<IActionResult> Pdf(Guid responseId)
    {
        try
        {
            var response = await responses.GetByIdAsync(responseId);
            if (response is null) return NotFound(new { ok = false, message = "Resultado não encontrado.", correlationId = HttpContext.TraceIdentifier });
            var certificate = await certificates.GetByResponseAsync(responseId);
            var payload = BuildSafeCertificatePayload(responseId, response, certificate, "pdf-json-fallback");
            logger.LogInformation("Certificate PDF fallback generated. ResponseId={ResponseId} CorrelationId={CorrelationId}", responseId, HttpContext.TraceIdentifier);
            return Ok(payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Certificate PDF generation failed. ResponseId={ResponseId} Error={Error}", responseId, LogSanitizer.SanitizeError(ex));
            throw;
        }
    }

    [HttpGet("/responses/{responseId:guid}/certificate.png")]
    public async Task<IActionResult> Png(Guid responseId)
    {
        try
        {
            var response = await responses.GetByIdAsync(responseId);
            if (response is null) return NotFound(new { ok = false, message = "Resultado não encontrado.", correlationId = HttpContext.TraceIdentifier });
            var certificate = await certificates.GetByResponseAsync(responseId);
            var payload = BuildSafeCertificatePayload(responseId, response, certificate, "png-json-fallback");
            logger.LogInformation("Certificate PNG fallback generated. ResponseId={ResponseId} CorrelationId={CorrelationId}", responseId, HttpContext.TraceIdentifier);
            return Ok(payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Certificate PNG generation failed. ResponseId={ResponseId} Error={Error}", responseId, LogSanitizer.SanitizeError(ex));
            throw;
        }
    }


    [HttpGet("/certificates/{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        try
        {
            var certificate = await certificates.GetByResponseAsync(id);
            if (certificate is null) return NotFound(new { ok = false, message = "Certificado não encontrado.", correlationId = HttpContext.TraceIdentifier });
            return Ok(new { ok = true, certificate, correlationId = HttpContext.TraceIdentifier });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Certificate fetch failed. Id={Id} Error={Error}", id, LogSanitizer.SanitizeError(ex));
            throw;
        }
    }

    [HttpPost("/certificates/{responseId:guid}/generate")]
    public async Task<IActionResult> Generate(Guid responseId)
    {
        try
        {
            var response = await responses.GetByIdAsync(responseId);
            if (response is null) return NotFound(new { ok = false, message = "Resposta não encontrada.", correlationId = HttpContext.TraceIdentifier });
            var certificate = await certificates.GetByResponseAsync(responseId);
            var payload = BuildSafeCertificatePayload(responseId, response, certificate, "json");
            logger.LogInformation("Certificate metadata generated. ResponseId={ResponseId} CorrelationId={CorrelationId}", responseId, HttpContext.TraceIdentifier);
            return Ok(payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Certificate generation failed. ResponseId={ResponseId} Error={Error}", responseId, LogSanitizer.SanitizeError(ex));
            throw;
        }
    }

    [HttpGet("/certificates/{certificateCode}/validate")]
    [HttpGet("/certificates/validate/{certificateCode}")]
    public async Task<IActionResult> Validate(string certificateCode)
    {
        if (string.IsNullOrWhiteSpace(certificateCode) || certificateCode.Length < 8)
            return BadRequest(new { ok = false, code = "INVALID_CERTIFICATE_CODE", message = "Código de validação inválido.", correlationId = HttpContext.TraceIdentifier });
        logger.LogInformation("Certificate validation requested. CodeHash={CodeHash} CorrelationId={CorrelationId}", LogSanitizer.HashForLog(certificateCode), HttpContext.TraceIdentifier);
        var normalized = certificateCode.Trim();
        var rawGuid = normalized.StartsWith("VALORA-", StringComparison.OrdinalIgnoreCase) ? normalized[7..] : normalized.StartsWith("VAL-", StringComparison.OrdinalIgnoreCase) ? normalized[4..] : normalized;
        if (!Guid.TryParse(rawGuid, out var responseId) && rawGuid.Length == 32) Guid.TryParseExact(rawGuid, "N", out responseId);
        if (responseId == Guid.Empty) return Ok(new { ok = true, valid = false, correlationId = HttpContext.TraceIdentifier });
        var response = await responses.GetByIdAsync(responseId);
        if (response is null) return Ok(new { ok = true, valid = false, correlationId = HttpContext.TraceIdentifier });
        var cert = await certificates.GetByResponseAsync(responseId);
        var result = await HttpContext.RequestServices.GetRequiredService<IResultRepository>().GetByResponseAsync(responseId);
        return Ok(new { ok = true, valid = true, participantName = cert?.ParticipantName ?? response.ParticipantName ?? "Participante", participantEmailMasked = LogSanitizer.MaskEmail(response.ParticipantEmail), surveyTitle = cert?.SurveyName ?? "Diagnóstico Valora Insight", completedAt = response.CompletedAt, score = result?.Percentage ?? result?.TotalScore ?? 0, maturityLevel = cert?.MaturityLabel ?? result?.MaturityLabel ?? "Em estruturação", correlationId = HttpContext.TraceIdentifier });
    }

    private object BuildSafeCertificatePayload(Guid responseId, Valora.Application.ReadModels.ResponseReadModel response, Valora.Application.ReadModels.CertificateReadModel? certificate, string format)
    {
        var participant = string.IsNullOrWhiteSpace(certificate?.ParticipantName) ? response.ParticipantName ?? "Participante" : certificate.ParticipantName;
        var company = string.IsNullOrWhiteSpace(certificate?.IssuerName) ? "Valora Pulse" : certificate.IssuerName;
        var survey = string.IsNullOrWhiteSpace(certificate?.SurveyName) ? "Pesquisa Valora" : certificate.SurveyName;
        var level = string.IsNullOrWhiteSpace(certificate?.MaturityLabel) ? "metadata-ready" : certificate.MaturityLabel;
        var code = string.IsNullOrWhiteSpace(certificate?.CertificateCode) ? $"VAL-{responseId:N}" : certificate.CertificateCode;
        return new { ok = true, format, fallback = true, message = "Geração binária ainda não habilitada neste ambiente; metadados seguros retornados em JSON.", responseId, participant, company, survey, score = "metadata-ready", level, issuedAt = certificate?.IssuedAt ?? response.CompletedAt ?? DateTime.UtcNow, validationCode = code, correlationId = HttpContext.TraceIdentifier };
    }
}
