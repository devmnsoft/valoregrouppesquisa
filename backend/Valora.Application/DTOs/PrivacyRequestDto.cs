namespace Valora.Application.DTOs;

public sealed record PrivacyRequestDto(Guid Id,Guid? OrganizationId,string RequesterEmailMasked,string RequestType,string Protocol,string Status,string? Description,Guid? ResponseId,DateTimeOffset RequestedAt,DateTimeOffset? CompletedAt,string? ResultJson);
