namespace ValoraPesquisa.Application.DTOs;
public record OrganizationDto(Guid Id,string Name,string PublicName,string Slug,string? Document,string Email,string? Phone,string Status,string PlanCode,DateTimeOffset CreatedAt,DateTimeOffset? UpdatedAt);
