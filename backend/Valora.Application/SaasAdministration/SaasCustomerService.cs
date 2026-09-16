using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace Valora.Application.SaasAdministration;

public sealed record SaasCustomerDto(Guid Id, Guid OrganizationId, string LegalName, string TradeName, string TaxIdNormalized, string PlanCode, string Status, DateTimeOffset CreatedAt);
public sealed record SaasCustomerListItem(Guid Id, Guid OrganizationId, string LegalName, string TradeName, string TaxIdMasked, string PlanCode, string Status, DateTimeOffset CreatedAt, int ActiveUserCount, string[] ActiveModules, DateTimeOffset? LastActivityAt);
public sealed record SaasCustomerListQuery(string? Search = null, string? Status = null, string? Module = null, int Page = 1, int PageSize = 20, string Sort = "name", bool Descending = false);
public sealed record SaasCustomerPage(IReadOnlyList<SaasCustomerListItem> Items, int TotalCount, int Page, int PageSize) {
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
public sealed record CreateSaasCustomerRequest(
    [property: Required, StringLength(200)] string LegalName,
    [property: Required, StringLength(160)] string TradeName,
    [property: Required] string TaxId,
    [property: Required, StringLength(60)] string PlanCode,
    Guid OrganizationId);

public interface ISaasCustomerRepository {
    Task<IReadOnlyList<SaasCustomerDto>> ListAsync(CancellationToken cancellationToken);
    Task<SaasCustomerPage> ListPageAsync(SaasCustomerListQuery query, CancellationToken cancellationToken);
    Task<SaasCustomerDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<SaasCustomerDto> CreateAsync(Guid id, CreateSaasCustomerRequest request, string normalizedTaxId, CancellationToken cancellationToken);
    Task<bool> SetBlockedAsync(Guid id, bool blocked, Guid actorUserId, string reason, string correlationId, CancellationToken cancellationToken);
}

public sealed class SaasCustomerService(ISaasCustomerRepository repository, ILogger<SaasCustomerService> logger) {
    public Task<IReadOnlyList<SaasCustomerDto>> ListAsync(CancellationToken cancellationToken) => repository.ListAsync(cancellationToken);
    public Task<SaasCustomerPage> ListPageAsync(SaasCustomerListQuery query, CancellationToken cancellationToken) {
        var status = string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim().ToLowerInvariant();
        if (status is not null && status is not ("active" or "blocked" or "inactive")) throw new ValidationException("Situação de cliente inválida.");
        var normalized = query with { Search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(), Status = status, Module = string.IsNullOrWhiteSpace(query.Module) ? null : query.Module.Trim().ToLowerInvariant(), Page = Math.Max(1, query.Page), PageSize = Math.Clamp(query.PageSize, 10, 100), Sort = query.Sort is "created" or "activity" ? query.Sort : "name" };
        return repository.ListPageAsync(normalized, cancellationToken);
    }
    public Task<SaasCustomerDto?> GetAsync(Guid id, CancellationToken cancellationToken) => repository.GetAsync(RequiredId(id, nameof(id)), cancellationToken);

    public async Task<SaasCustomerDto> CreateAsync(CreateSaasCustomerRequest request, CancellationToken cancellationToken) {
        RequiredId(request.OrganizationId, nameof(request.OrganizationId));
        Validator.ValidateObject(request, new ValidationContext(request), true);
        var taxId = NormalizeTaxId(request.TaxId);
        if (taxId.Length is not (11 or 14)) throw new ValidationException("Informe um CPF ou CNPJ válido, somente com seus 11 ou 14 dígitos.");
        var customer = await repository.CreateAsync(Guid.NewGuid(), request, taxId, cancellationToken);
        logger.LogInformation("SaaS customer {CustomerId} created for organization {OrganizationId}", customer.Id, customer.OrganizationId);
        return customer;
    }

    public async Task<bool> SetBlockedAsync(Guid id, bool blocked, Guid actorUserId, string reason, string correlationId, CancellationToken cancellationToken) {
        RequiredId(id, nameof(id)); RequiredId(actorUserId, nameof(actorUserId));
        if (string.IsNullOrWhiteSpace(reason)) throw new ValidationException("Informe o motivo da alteração de acesso.");
        var changed = await repository.SetBlockedAsync(id, blocked, actorUserId, reason.Trim(), correlationId, cancellationToken);
        logger.LogWarning("SaaS customer {CustomerId} access changed to {Status} by {ActorUserId}", id, blocked ? "blocked" : "active", actorUserId);
        return changed;
    }

    private static string NormalizeTaxId(string value) => new(value.Where(char.IsDigit).ToArray());
    private static Guid RequiredId(Guid value, string name) => value == Guid.Empty ? throw new ValidationException($"{name} inválido.") : value;
}
