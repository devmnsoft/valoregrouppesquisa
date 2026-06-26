namespace Valora.Application.DTOs;

public record AuthResponse(string Token, object User, object? Organization, object? Plan);
