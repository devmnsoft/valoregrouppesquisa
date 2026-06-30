namespace ValoraPesquisa.Application.DTOs;
public record UserDto(Guid Id,Guid? OrganizationId,string Name,string Email,string Role,string Status,string? Phone,DateTimeOffset? LastLoginAt,DateTimeOffset CreatedAt,DateTimeOffset? UpdatedAt);
