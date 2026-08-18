namespace Valora.Application.Contracts;

public interface IAssistedOperationsRepository
{
    Task<IReadOnlyList<IDictionary<string, object?>>> ListAsync(string resource, Guid? organizationId, CancellationToken ct = default);
    Task<IDictionary<string, object?>?> GetAsync(string resource, Guid id, Guid? organizationId, CancellationToken ct = default);
    Task<Guid> CreateAsync(string resource, Guid? organizationId, Guid? userId, IReadOnlyDictionary<string, object?> values, string correlationId, CancellationToken ct = default);
    Task<bool> UpdateAsync(string resource, Guid id, Guid? organizationId, IReadOnlyDictionary<string, object?> values, string action, string correlationId, CancellationToken ct = default);
    Task<IReadOnlyList<IDictionary<string, object?>>> CustomerHealthAsync(Guid? organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<IDictionary<string, object?>>> UsageAsync(Guid? organizationId, CancellationToken ct = default);
    Task<Guid> RunDataQualityAsync(Guid? userId, string correlationId, CancellationToken ct = default);
}
