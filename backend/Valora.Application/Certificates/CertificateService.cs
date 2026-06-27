using Microsoft.Extensions.Logging;

namespace Valora.Application.Certificates;
public sealed class CertificateService(ILogger<CertificateService> logger) { public object Plan(Guid responseId, string format) { logger.LogInformation("Certificate plan requested. ResponseId={ResponseId} Format={Format}", responseId, format); return new { responseId, format, validationCode = $"VALORA-{responseId:N}"[..19], status = "planned" }; } }
