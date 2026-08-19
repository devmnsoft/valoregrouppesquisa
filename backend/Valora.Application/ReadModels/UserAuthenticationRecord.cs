namespace Valora.Application.ReadModels;

public sealed record UserAuthenticationRecord(
    Guid Id,
    Guid? OrganizationId,
    string Name,
    string Email,
    string PasswordHash,
    string Status,
    string? Phone,
    string RoleCodesCsv,
    DateTimeOffset? DeletedAt = null)
{
    public IReadOnlyList<string> RoleCodes => (RoleCodesCsv ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
