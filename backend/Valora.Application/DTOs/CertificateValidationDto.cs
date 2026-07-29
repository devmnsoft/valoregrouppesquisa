namespace Valora.Application.DTOs;

public sealed record CertificateValidationDto(bool Valid,string Status,string ValidationCode,string? ParticipantName,string? CompanyName,string? Level,DateTimeOffset? IssuedAt);
