namespace Valora.Application.DTOs;
public sealed record PublicResponseDto(Guid Id,string? ParticipantName,string? ParticipantEmail,string Status,DateTime? CompletedAt);
