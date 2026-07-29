namespace Valora.Application.DTOs;

public sealed record AuthenticatedOrganizationDto(Guid Id, string Name, string? TradeName, string Slug);
