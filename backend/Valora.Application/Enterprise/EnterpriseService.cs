using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Exceptions;
using Valora.Domain.Operations;

namespace Valora.Application.Enterprise;

public sealed class EnterpriseService(IEnterpriseRepository repository, IAuditRepository audit)
{
    public Task<PortfolioSummary> SummaryAsync(CancellationToken ct) => repository.SummaryAsync(ct);
    public Task<EnterprisePage<PortfolioCompany>> CompaniesAsync(EnterpriseListQuery q, CancellationToken ct) => repository.CompaniesAsync(Normalize(q), ct);
    public Task<EnterprisePage<CrmLead>> LeadsAsync(EnterpriseListQuery q, CancellationToken ct) => repository.LeadsAsync(Normalize(q), ct);
    public Task<IReadOnlyList<EnterpriseItem>> ItemsAsync(Guid? organizationId, string kind, CancellationToken ct) => repository.ListItemsAsync(organizationId, ValidateKind(kind), ct);

    public async Task<Guid> CreateLeadAsync(string name, string? company, string? email, string? phone, string? plan, string? owner, DateTime? nextAction, string? notes, Guid userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationAppException("Informe o nome do lead.");
        var id = await repository.CreateLeadAsync(new(Guid.NewGuid(), name.Trim(), company?.Trim(), email?.Trim(), phone?.Trim(), "new", plan, owner, nextAction, notes, DateTime.UtcNow), ct);
        await audit.AddAsync(new AuditEntry(null, userId, "crm.lead.created", "crm_lead", id.ToString(), "Lead comercial criado", "{}"));
        return id;
    }

    public async Task ChangeCompanyStatusAsync(Guid id, string status, Guid userId, CancellationToken ct)
    {
        string[] allowed = ["active", "onboarding", "at_risk", "blocked", "cancelled", "trial", "delinquent"];
        if (!allowed.Contains(status)) throw new ValidationAppException("Status da empresa inválido.");
        await repository.UpdateCompanyStatusAsync(id, status, ct);
        await audit.AddAsync(new AuditEntry(id, userId, "company.status.changed", "organization", id.ToString(), "Status da empresa alterado", JsonSerializer.Serialize(new { status })));
    }

    public async Task<Guid> SaveItemAsync(Guid? organizationId, Guid? id, UpsertEnterpriseItemRequest request, Guid userId, CancellationToken ct)
    {
        var normalized = request with { Kind = ValidateKind(request.Kind), Name = request.Name.Trim(), Status = request.Status.Trim().ToLowerInvariant() };
        if (normalized.Name.Length is < 2 or > 160) throw new ValidationAppException("Informe um nome entre 2 e 160 caracteres.");
        ValidateOperationalItem(normalized);
        var itemId = await repository.UpsertItemAsync(organizationId, id, normalized, ct);
        await audit.AddAsync(new AuditEntry(organizationId, userId, $"enterprise.{normalized.Kind}.saved", normalized.Kind, itemId.ToString(), "Configuração enterprise atualizada", "{}"));
        return itemId;
    }

    public async Task<ApiKeyIssued> CreateApiKeyAsync(Guid organizationId, string name, IReadOnlyList<string> scopes, Guid userId, CancellationToken ct)
    {
        var allowed = new HashSet<string>(["surveys:read", "results:read", "reports:read", "certificates:read", "units:read", "departments:read"]);
        if (scopes.Count == 0 || scopes.Any(x => !allowed.Contains(x))) throw new ValidationAppException("Selecione apenas escopos válidos.");
        var secret = "vli_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret))).ToLowerInvariant();
        var issued = await repository.CreateApiKeyAsync(organizationId, name.Trim(), scopes, hash, secret[..12], secret, ct);
        await audit.AddAsync(new AuditEntry(organizationId, userId, "api_key.created", "api_key", issued.Id.ToString(), "Chave de API criada", JsonSerializer.Serialize(new { issued.Prefix, Scopes = scopes })));
        return issued;
    }

    public CsvPreview PreviewCsv(string type, string csv)
    {
        string[] types = ["companies", "users", "units", "departments", "surveys", "responses", "members", "respondents", "organization-structure"];
        if (!types.Contains(type)) throw new ValidationAppException("Tipo de importação inválido.");
        var lines = csv.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) throw new ValidationAppException("O CSV precisa conter cabeçalho e pelo menos uma linha.");
        var headers = lines[0].Split(';').Select(x => x.Trim().ToLowerInvariant()).ToArray();
        var rows = lines.Skip(1).Take(500).Select((line, index) => {
            var values = line.Split(';'); var errors = new List<string>();
            if (values.Length != headers.Length) errors.Add("Quantidade de colunas diferente do cabeçalho.");
            var data = headers.Select((h, i) => (h, value: i < values.Length ? values[i].Trim() : "")).ToDictionary(x => x.h, x => x.value);
            errors.AddRange(MigrationSafetyPolicy.Validate(type, data));
            return new CsvPreviewRow(index + 2, data, errors);
        }).ToList();
        var token = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(type + "|" + csv))).ToLowerInvariant();
        return new(type, rows, rows.Count(x => x.Errors.Count == 0), rows.Count(x => x.Errors.Count > 0), token);
    }

    private static EnterpriseListQuery Normalize(EnterpriseListQuery q) => q with { Page = Math.Max(1, q.Page), PageSize = Math.Clamp(q.PageSize, 10, 100) };
    private static void ValidateOperationalItem(UpsertEnterpriseItemRequest item)
    {
        var statuses = item.Kind switch
        {
            "implementation" => new[] { "planned", "in_progress", "blocked", "homologation", "completed", "cancelled" },
            "production-checklist" => new[] { "pending", "in_progress", "completed", "blocked", "waived" },
            "backup" => new[] { "current", "delayed", "failed", "not_configured", "validating" },
            "lgpd-request" => new[] { "received", "under_review", "processing", "completed", "justifiably_refused" },
            _ => Array.Empty<string>()
        };
        if (statuses.Length > 0 && !statuses.Contains(item.Status)) throw new ValidationAppException("Status inválido para este módulo.");
        if (item.Kind == "implementation")
        {
            string[] required = ["organizationId", "internalOwner", "clientOwner", "plan", "startDate", "targetDate", "steps"];
            if (required.Any(x => !item.Configuration.TryGetProperty(x, out _))) throw new ValidationAppException("Preencha empresa, responsáveis, plano, datas e etapas da implantação.");
            if (!item.Configuration.GetProperty("steps").EnumerateArray().Any()) throw new ValidationAppException("A implantação precisa conter etapas.");
        }
        if (item.Kind == "release-note" && (!item.Configuration.TryGetProperty("version", out var version) || string.IsNullOrWhiteSpace(version.GetString())))
            throw new ValidationAppException("Informe a versão da release.");
        if (item.Kind == "backup" && item.Configuration.TryGetProperty("providerSecret", out _))
            throw new ValidationAppException("Segredos de backup devem ser configurados no provedor de infraestrutura, nunca nesta tela.");
    }
    private static string ValidateKind(string kind) => kind.Trim().ToLowerInvariant() switch
    {
        "plan" or "subscription" or "integration" or "template" or "alert" or "automation" or "branding"
        or "implementation" or "production-checklist" or "backup" or "release-note" or "data-quality"
        or "permission-governance" or "plan-governance" or "lgpd-request" => kind.Trim().ToLowerInvariant(),
        _ => throw new ValidationAppException("Módulo de governança inválido.")
    };
}
