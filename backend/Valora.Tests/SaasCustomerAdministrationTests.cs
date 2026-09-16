using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging.Abstractions;
using Valora.Application.SaasAdministration;

namespace Valora.Tests;

public sealed class SaasCustomerAdministrationTests {
    [Fact]
    public async Task Customer_query_is_bounded_normalized_and_forwarded_to_repository() {
        var repository = new RecordingRepository();
        var service = new SaasCustomerService(repository, NullLogger<SaasCustomerService>.Instance);

        await service.ListPageAsync(new("  Acme  ", "ACTIVE", " FORMS ", -2, 1000, "untrusted", true), default);

        Assert.Equal(new SaasCustomerListQuery("Acme", "active", "forms", 1, 100, "name", true), repository.Query);
    }

    [Fact]
    public async Task Customer_query_rejects_unknown_status() {
        var service = new SaasCustomerService(new RecordingRepository(), NullLogger<SaasCustomerService>.Instance);
        await Assert.ThrowsAsync<ValidationException>(() => service.ListPageAsync(new(Status: "deleted"), default));
    }

    [Fact]
    public void Customer_listing_contract_uses_server_pagination_masking_and_real_activity() {
        var root = RepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "backend/Valora.Infrastructure/SaasAdministration/SaasCustomerRepository.cs"));
        Assert.Contains("OFFSET @offset LIMIT @pageSize", source);
        Assert.Contains("TaxIdMasked", source);
        Assert.Contains("saas_customer_audit_events", source);
        Assert.Contains("saas_customer_users", source);
        Assert.Contains("saas_customer_modules", source);
        Assert.DoesNotContain("ORDER BY {query.Sort}", source);
    }

    private sealed class RecordingRepository : ISaasCustomerRepository {
        public SaasCustomerListQuery? Query { get; private set; }
        public Task<SaasCustomerPage> ListPageAsync(SaasCustomerListQuery query, CancellationToken cancellationToken) { Query = query; return Task.FromResult(new SaasCustomerPage([], 0, query.Page, query.PageSize)); }
        public Task<IReadOnlyList<SaasCustomerDto>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<SaasCustomerDto>>([]);
        public Task<SaasCustomerDto?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<SaasCustomerDto?>(null);
        public Task<SaasCustomerDto> CreateAsync(Guid id, CreateSaasCustomerRequest request, string normalizedTaxId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> SetBlockedAsync(Guid id, bool blocked, Guid actorUserId, string reason, string correlationId, CancellationToken cancellationToken) => Task.FromResult(false);
    }

    private static string RepositoryRoot() {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "backend"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
