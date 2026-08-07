using System.Text.Json;

namespace Valora.Application.Enterprise;

public sealed record EnterpriseListQuery(string? Search, string? Status, string? Plan, string? Health, DateOnly? From, DateOnly? To, int Page = 1, int PageSize = 25);
public sealed record EnterprisePage<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
public sealed record PortfolioCompany(Guid Id, string Name, string? Cnpj, string? Email, string Status, string Health, string? Plan, DateTime CreatedAt, DateTime? LastActivityAt, int UsagePercent);
public sealed record PortfolioSummary(int Companies, int AtRisk, int TrialsEnding, int NearLimit, int ActiveLeads, decimal MonthlyRecurringRevenue);
public sealed record CrmLead(Guid Id, string Name, string? CompanyName, string? Email, string? Phone, string Status, string? IntendedPlan, string? Owner, DateTime? NextActionAt, string? Notes, DateTime CreatedAt);
public sealed record EnterpriseItem(Guid Id, Guid? OrganizationId, string Kind, string Name, string Status, JsonElement Configuration, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record UpsertEnterpriseItemRequest(string Kind, string Name, string Status, JsonElement Configuration);
public sealed record CsvPreviewRow(int Line, IReadOnlyDictionary<string, string> Values, IReadOnlyList<string> Errors);
public sealed record CsvPreview(string Type, IReadOnlyList<CsvPreviewRow> Rows, int Valid, int Invalid, string ConfirmationToken);
public sealed record ApiKeyIssued(Guid Id, string Name, string Prefix, string Secret, IReadOnlyList<string> Scopes, DateTime CreatedAt);

public interface IEnterpriseRepository
{
    Task<PortfolioSummary> SummaryAsync(CancellationToken ct);
    Task<EnterprisePage<PortfolioCompany>> CompaniesAsync(EnterpriseListQuery query, CancellationToken ct);
    Task<EnterprisePage<CrmLead>> LeadsAsync(EnterpriseListQuery query, CancellationToken ct);
    Task<Guid> CreateLeadAsync(CrmLead lead, CancellationToken ct);
    Task UpdateCompanyStatusAsync(Guid id, string status, CancellationToken ct);
    Task<IReadOnlyList<EnterpriseItem>> ListItemsAsync(Guid? organizationId, string kind, CancellationToken ct);
    Task<Guid> UpsertItemAsync(Guid? organizationId, Guid? id, UpsertEnterpriseItemRequest request, CancellationToken ct);
    Task SetItemStatusAsync(Guid? organizationId, Guid id, string status, CancellationToken ct);
    Task<ApiKeyIssued> CreateApiKeyAsync(Guid organizationId, string name, IReadOnlyList<string> scopes, string hash, string prefix, string secret, CancellationToken ct);
}
