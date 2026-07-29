namespace Valora.Application.DTOs;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
