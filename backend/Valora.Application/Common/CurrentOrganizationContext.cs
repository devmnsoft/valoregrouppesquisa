namespace Valora.Application.Common;

public sealed record CurrentOrganizationContext(
    Guid? OrganizationId,
    bool IsResolved,
    string Source,
    bool RequiresSelection,
    string? ErrorMessage)
{
    public const string RequiredMessage = "Selecione uma organização para continuar.";

    public static CurrentOrganizationContext Resolved(Guid organizationId, string source) =>
        new(organizationId, true, source, false, null);

    public static CurrentOrganizationContext Unresolved(string source = "none", string? errorMessage = null) =>
        new(null, false, source, true, errorMessage ?? RequiredMessage);

    public Guid RequireOrganizationId() => OrganizationId
        ?? throw new UnauthorizedAccessException(RequiredMessage);
}

public interface ICurrentOrganizationProvider
{
    CurrentOrganizationContext GetCurrent();
}
