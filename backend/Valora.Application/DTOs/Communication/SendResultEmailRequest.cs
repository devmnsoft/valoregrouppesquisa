namespace Valora.Application.DTOs.Communication;
public sealed record SendResultEmailRequest(string To, string? Subject, string? Message, bool IncludeCertificate, string? ResultToken = null);
