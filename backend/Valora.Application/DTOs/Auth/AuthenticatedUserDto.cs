namespace Valora.Application.DTOs;

public sealed record AuthenticatedUserDto(Guid Id, string Name, string Email, string Role);
