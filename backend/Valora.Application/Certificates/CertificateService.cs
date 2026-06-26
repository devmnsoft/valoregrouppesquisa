namespace Valora.Application.Certificates;
public sealed class CertificateService { public object Plan(Guid responseId, string format) => new { responseId, format, validationCode = $"VALORA-{responseId:N}"[..19], status = "planned" }; }
