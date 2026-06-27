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

    [HttpGet("/certificates/{certificateCode}/validate")]
    public IActionResult Validate(string certificateCode)
    {
        if (string.IsNullOrWhiteSpace(certificateCode) || certificateCode.Length < 8)
            return BadRequest(new { ok = false, code = "INVALID_CERTIFICATE_CODE", message = "Código de validação inválido.", correlationId = HttpContext.TraceIdentifier });
        logger.LogInformation("Certificate validation requested. CodeHash={CodeHash} CorrelationId={CorrelationId}", LogSanitizer.HashForLog(certificateCode), HttpContext.TraceIdentifier);
        return Ok(new { ok = true, status = "metadata-validation-ready", certificateCode = certificateCode.Trim(), correlationId = HttpContext.TraceIdentifier });
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
