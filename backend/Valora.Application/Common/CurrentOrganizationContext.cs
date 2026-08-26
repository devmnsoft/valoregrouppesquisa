namespace Valora.Application.Common;

public sealed record CurrentOrganizationContext(
    Guid OrganizationId,
    bool IsResolved,
    string? Source,
    string? ErrorMessage)
{
    public const string RequiredMessage = "Selecione uma organização para consultar os formulários.";

    public static CurrentOrganizationContext Resolved(Guid organizationId, string source) =>
        new(organizationId, true, source, null);

    public static CurrentOrganizationContext Unresolved(string? errorMessage = null) =>
        new(Guid.Empty, false, null, errorMessage ?? RequiredMessage);
}

public interface ICurrentOrganizationProvider
{
    CurrentOrganizationContext GetCurrent();
}
