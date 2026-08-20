using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Valora.Application.Integrations;

public static class IntegrationScopes
{
    public const string OrganizationsRead = "organizations.read";
    public const string DiagnosticsRead = "diagnostics.read";
    public const string ReportsRead = "reports.read";
    public const string CertificatesValidate = "certificates.validate";
    public const string BenchmarkRead = "benchmark.read";
    public const string EvolutionRead = "evolution.read";
    public const string BiRead = "bi.read";
}

public sealed record AuthenticatedApiKey(Guid Id, Guid OrganizationId, IReadOnlySet<string> Scopes);
public sealed record PublicDataResult(Guid OrganizationId, object Data);
public sealed record WebhookEnvelope(Guid Id, string Type, Guid OrganizationId, DateTime OccurredAt, object Data);
public sealed record LookupResult(bool Available, string? Message, IReadOnlyDictionary<string, string> Fields);
public sealed record ImportValidationRow(int Line, IReadOnlyDictionary<string, string> Values, IReadOnlyList<string> Errors);

public interface IIntegrationRepository
{
    Task<AuthenticatedApiKey?> AuthenticateAsync(string hash, CancellationToken ct);
    Task RecordApiUseAsync(AuthenticatedApiKey? key, string prefix, string endpoint, int status, string? scope, string correlationId, CancellationToken ct);
    Task<PublicDataResult?> PublicDataAsync(string resource, Guid id, CancellationToken ct);
    Task<PublicDataResult?> CertificateAsync(string code, CancellationToken ct);
    Task<Guid> EnqueueEmailAsync(Guid organizationId, string template, string recipient, object payload, CancellationToken ct);
    Task<Guid> CreateImportAsync(Guid organizationId, string type, string format, string checksum, IReadOnlyList<ImportValidationRow> rows, CancellationToken ct);
}

public sealed class ApiKeyAuthenticator(IIntegrationRepository repository)
{
    public async Task<AuthenticatedApiKey?> AuthenticateAsync(string? presentedKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(presentedKey) || !presentedKey.StartsWith("vli_", StringComparison.Ordinal)) return null;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(presentedKey))).ToLowerInvariant();
        return await repository.AuthenticateAsync(hash, ct);
    }

    public static bool HasScope(AuthenticatedApiKey key, string required) => key.Scopes.Contains(required) || key.Scopes.Contains("*");
}

public static class WebhookSigner
{
    public static string Sign(string secret, string payload) => "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    public static TimeSpan RetryDelay(int attempt) => TimeSpan.FromMinutes(Math.Min(60, Math.Pow(2, Math.Clamp(attempt, 0, 6))));
}

public interface ICnpjLookupService { Task<LookupResult> LookupAsync(string cnpj, CancellationToken ct); }
public interface ICepLookupService { Task<LookupResult> LookupAsync(string cep, CancellationToken ct); }

public sealed class DisabledCnpjLookupService : ICnpjLookupService
{
    public Task<LookupResult> LookupAsync(string cnpj, CancellationToken ct) => Task.FromResult(new LookupResult(false, "Consulta automática de CNPJ indisponível. Continue com o preenchimento manual.", new Dictionary<string, string>()));
}
public sealed class DisabledCepLookupService : ICepLookupService
{
    public Task<LookupResult> LookupAsync(string cep, CancellationToken ct) => Task.FromResult(new LookupResult(false, "Consulta automática de CEP indisponível. O endereço pode ser preenchido manualmente.", new Dictionary<string, string>()));
}

public sealed class ExternalImportValidator
{
    private static readonly IReadOnlyDictionary<string, string[]> Required = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["areas"] = ["nome"], ["respondents"] = ["nome", "email"], ["units"] = ["nome"], ["roles"] = ["nome"], ["indicators"] = ["nome", "valor"]
    };

    public IReadOnlyList<ImportValidationRow> ValidateCsv(string type, string csv)
    {
        if (!Required.TryGetValue(type, out var required)) throw new ArgumentException("Tipo de importação não suportado.", nameof(type));
        var lines = csv.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) return [new(1, new Dictionary<string, string>(), ["Arquivo vazio."])];
        var headers = lines[0].Split(';').Select(x => x.Trim().ToLowerInvariant()).ToArray();
        return lines.Skip(1).Select((line, index) =>
        {
            var values = line.Split(';');
            var data = headers.Select((h, i) => (h, value: i < values.Length ? values[i].Trim() : "")).ToDictionary(x => x.h, x => x.value);
            var errors = required.Where(x => !data.TryGetValue(x, out var value) || string.IsNullOrWhiteSpace(value)).Select(x => $"Campo obrigatório ausente: {x}.").ToList();
            if (values.Length != headers.Length) errors.Add("Quantidade de colunas diferente do cabeçalho.");
            return new ImportValidationRow(index + 2, data, errors);
        }).ToList();
    }
}
