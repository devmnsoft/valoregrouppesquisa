namespace Valora.Application.DTOs;

public sealed record ExportJobDto(Guid Id,Guid OrganizationId,Guid? RequestedBy,string Entity,string Format,string Status,string? ResultFileName,string? ResultMimeType,string? ResultPayload,DateTimeOffset CreatedAt,DateTimeOffset? CompletedAt,string? ErrorMessage);
