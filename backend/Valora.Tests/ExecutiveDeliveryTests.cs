using System.ComponentModel.DataAnnotations;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.FormalDeliverables;

namespace Valora.Tests;

public sealed class ExecutiveDeliveryTests {
    [Fact]
    public void Prepare_ValidatesEmptyTitle() {
        var request = new PrepareDeliverableRequest {
            DiagnosticId = Guid.NewGuid(),
            ResultId = Guid.NewGuid(),
            Title = "  ",
            CommandId = "cmd-1"
        };
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(request, new ValidationContext(request), results, true);
        Assert.False(valid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PrepareDeliverableRequest.Title)));
    }

    [Fact]
    public async Task Prepare_ServiceRejectsBlankTitle() {
        var service = CreateService(new FakeRepo());
        await Assert.ThrowsAsync<Valora.Application.Exceptions.ValidationAppException>(() =>
            service.PrepareAsync(Guid.NewGuid(), Guid.NewGuid(), new PrepareDeliverableRequest {
                DiagnosticId = Guid.NewGuid(),
                ResultId = Guid.NewGuid(),
                Title = "",
                CommandId = "cmd-blank"
            }));
    }

    [Fact]
    public async Task Publish_DetectsResultHashDivergence() {
        var org = Guid.NewGuid();
        var user = Guid.NewGuid();
        var resultId = Guid.NewGuid();
        var diagnosticId = Guid.NewGuid();
        var deliverableId = Guid.NewGuid();
        var original = Eligible(resultId, diagnosticId, 10, 100);
        var diverged = Eligible(resultId, diagnosticId, 40, 100);
        var entity = Entity(org, deliverableId, resultId, diagnosticId, ExecutiveDeliveryService.ComputeResultHash(original),
            DeliverableEditorialStatuses.InReview, DeliverableProcessingStatuses.AwaitingGeneration, user);
        var repo = new FakeRepo { Entity = entity, Eligible = diverged };
        var service = CreateService(repo, userId: user);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.PublishAsync(org, user, deliverableId, new PublishDeliverableRequest {
                Confirmed = true,
                CommandId = "pub-1",
                VersionNumber = 1
            }));
        Assert.Contains("divergência", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Download_BlockedWhenNotAvailable() {
        var org = Guid.NewGuid();
        var user = Guid.NewGuid();
        var deliverableId = Guid.NewGuid();
        var entity = Entity(org, deliverableId, Guid.NewGuid(), Guid.NewGuid(), "abc",
            DeliverableEditorialStatuses.Draft, DeliverableProcessingStatuses.AwaitingGeneration, user);
        var service = CreateService(new FakeRepo { Entity = entity }, userId: user);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DownloadAsync(org, user, deliverableId));
        Assert.Contains("disponível", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Certificate_MaturityCriteriaPendingWhenUndefined() {
        var org = Guid.NewGuid();
        var resultId = Guid.NewGuid();
        var repo = new FakeRepo {
            Eligible = Eligible(resultId, Guid.NewGuid(), 50, 100),
            Template = new DeliverableTemplateInfo("certificate_maturity", "Certificado de Maturidade", DeliverableTypes.Certificate,
                """{"certificateType":"maturity","formalMaturityCertification":false}""")
        };
        var service = CreateService(repo);
        var result = await service.CertificateEligibilityAsync(org, resultId, "certificate_maturity");
        Assert.False(result.Eligible);
        Assert.Equal("certificate.maturity_criteria_undefined", result.PendingRuleCode);
        Assert.Contains("não constitui certificação de maturidade", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Share_RevokePreventsResolve() {
        var repo = new FakeShareRepo();
        var audit = new AuditSpy();
        var service = new SecureShareLinkService(repo, audit);
        var created = await service.CreateAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TimeSpan.FromHours(2), true);
        Assert.NotNull(await service.ResolveAsync(created.Token, true));
        Assert.True(await service.RevokeAsync(repo.Last.OrganizationId, created.Id, Guid.NewGuid()));
        Assert.Null(await service.ResolveAsync(created.Token, true));
    }

    [Fact]
    public void Pdf_StillNonEmptyViaExistingExporter() {
        var snapshot = new DiagnosisDocumentSnapshot(
            Guid.NewGuid(), "Org", Guid.NewGuid(), "Diag", DateTimeOffset.UtcNow, 70m, "Estruturado",
            "Valora", "1.0", "Resumo", "Leitura", [new("Gov", 70m, "ok")], [], [], [], [], [], [], [], ["limite"], true);
        var file = new ExecutiveReportExportService().Render(snapshot, DeliverableFormat.Pdf, DateTimeOffset.UtcNow);
        Assert.True(file.Content.Length > 500);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(file.Content, 0, 4));
    }

    [Fact]
    public void EditorialAndProcessingStatuses_AreDistinct() {
        Assert.Contains(DeliverableEditorialStatuses.Published, DeliverableEditorialStatuses.All);
        Assert.Contains(DeliverableProcessingStatuses.Available, DeliverableProcessingStatuses.All);
        Assert.DoesNotContain(DeliverableProcessingStatuses.Available, DeliverableEditorialStatuses.All);
        Assert.DoesNotContain(DeliverableEditorialStatuses.Published, DeliverableProcessingStatuses.All);
        var item = new DeliverableListItemDto(Guid.NewGuid(), "T", null, null, null, DeliverableTypes.ExecutiveReport, 1,
            DeliverableEditorialStatuses.Published, DeliverableProcessingStatuses.Available, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            null, true, false, true, true, true, "trc");
        Assert.Equal(DeliverableEditorialStatuses.Published, item.EditorialStatus);
        Assert.Equal(DeliverableProcessingStatuses.Available, item.ProcessingStatus);
        Assert.True(item.CanDownload);
        Assert.True(item.CanShare);
    }

    private static IExecutiveDeliveryService CreateService(FakeRepo repo, Guid? userId = null) {
        var entitlements = new EntitlementStub();
        var permissions = new PermissionStub(true);
        var documents = new DocumentServiceStub();
        var store = new MemoryStore();
        var shares = new SecureShareLinkService(new FakeShareRepo(), new AuditSpy());
        return new ExecutiveDeliveryService(repo, documents, store, shares, entitlements, permissions, new AuditSpy());
    }

    private static EligibleResultInfo Eligible(Guid resultId, Guid diagnosticId, decimal total, decimal max) =>
        new(resultId, diagnosticId, "Diagnóstico", DateTimeOffset.UtcNow, total, max,
            max == 0 ? 0 : total / max * 100, DateTimeOffset.UtcNow, "Valora", "1.0");

    private static FormalDeliverableEntity Entity(Guid org, Guid id, Guid resultId, Guid diagnosticId, string hash,
        string editorial, string processing, Guid user) =>
        new(id, org, diagnosticId, resultId, DeliverableTypes.ExecutiveReport, "Relatório", editorial, editorial, processing,
            1, "executive_valora", "[]", null, user, hash, "Valora", "1.0", null, null, null, null, null, null, user, "{}",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    private sealed class FakeRepo : IFormalDeliverableRepository {
        public FormalDeliverableEntity? Entity { get; set; }
        public EligibleResultInfo? Eligible { get; set; }
        public DeliverableTemplateInfo? Template { get; set; }
        public Task<(IReadOnlyList<DeliverableListItemDto> Items, int Total)> ListAsync(Guid o, DeliverableListQuery q, CancellationToken c = default) =>
            Task.FromResult<(IReadOnlyList<DeliverableListItemDto>, int)>(([], 0));
        public Task<DeliverableDetailsDto?> GetDetailsAsync(Guid o, Guid id, CancellationToken c = default) => Task.FromResult<DeliverableDetailsDto?>(null);
        public Task<FormalDeliverableEntity?> GetEntityAsync(Guid o, Guid id, CancellationToken c = default) => Task.FromResult(Entity);
        public Task<FormalDeliverableEntity?> FindByCommandIdAsync(Guid o, string commandId, CancellationToken c = default) => Task.FromResult<FormalDeliverableEntity?>(null);
        public Task InsertAsync(FormalDeliverableEntity entity, CancellationToken c = default) { Entity = entity; return Task.CompletedTask; }
        public Task UpdateLifecycleAsync(FormalDeliverableEntity entity, CancellationToken c = default) { Entity = entity; return Task.CompletedTask; }
        public Task MarkSupersededAsync(Guid o, Guid id, CancellationToken c = default) => Task.CompletedTask;
        public Task WriteGenerationJobAsync(Guid o, Guid d, Guid? u, string s, string? e, CancellationToken c = default) => Task.CompletedTask;
        public Task WriteReportGenerationLogAsync(Guid o, Guid? d, Guid? r, Guid? u, string f, string s, string? detail, CancellationToken c = default) => Task.CompletedTask;
        public Task<EligibleResultInfo?> LoadEligibleResultAsync(Guid o, Guid resultId, CancellationToken c = default) => Task.FromResult(Eligible);
        public Task<IReadOnlyList<ResultOptionDto>> SearchEligibleResultsAsync(Guid o, string? s, int limit, CancellationToken c = default) => Task.FromResult<IReadOnlyList<ResultOptionDto>>([]);
        public Task<IReadOnlyList<TemplateOptionDto>> ListTemplatesAsync(string? type, CancellationToken c = default) => Task.FromResult<IReadOnlyList<TemplateOptionDto>>([]);
        public Task<DeliverableTemplateInfo?> GetTemplateAsync(string templateCode, CancellationToken c = default) => Task.FromResult(Template);
        public Task<IReadOnlyList<ReviewerOptionDto>> ListReviewersAsync(Guid o, string? s, int limit, CancellationToken c = default) => Task.FromResult<IReadOnlyList<ReviewerOptionDto>>([]);
        public Task<IReadOnlyList<DeliverableAccessEventDto>> ListAccessHistoryAsync(Guid o, Guid d, CancellationToken c = default) => Task.FromResult<IReadOnlyList<DeliverableAccessEventDto>>([]);
        public Task<IReadOnlyList<DeliverableShareLinkSummaryDto>> ListShareLinksAsync(Guid o, Guid d, CancellationToken c = default) => Task.FromResult<IReadOnlyList<DeliverableShareLinkSummaryDto>>([]);
    }

    private sealed class FakeShareRepo : IShareLinkRepository {
        private readonly Dictionary<string, ShareLink> _byHash = new(StringComparer.Ordinal);
        public ShareLink Last { get; private set; } = new(Guid.Empty, Guid.Empty, Guid.Empty, "", "", DateTimeOffset.UtcNow, false);
        public Task SaveAsync(ShareLink link, Guid? createdBy, CancellationToken cancellationToken = default) {
            Last = link;
            _byHash[link.TokenHash] = link;
            return Task.CompletedTask;
        }
        public Task<ShareLink?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            Task.FromResult(_byHash.TryGetValue(tokenHash, out var link) && link.RevokedAt is null ? link : null);
        public Task<ShareLink?> FindAnyByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            Task.FromResult(_byHash.TryGetValue(tokenHash, out var link) ? link : null);
        public Task RegisterAccessAsync(Guid linkId, bool downloadRequested, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> RevokeAsync(Guid organizationId, Guid linkId, CancellationToken cancellationToken = default) {
            foreach (var pair in _byHash.ToList()) {
                if (pair.Value.Id == linkId && pair.Value.OrganizationId == organizationId) {
                    _byHash[pair.Key] = pair.Value with { RevokedAt = DateTimeOffset.UtcNow };
                    return Task.FromResult(true);
                }
            }
            return Task.FromResult(false);
        }
    }

    private sealed class EntitlementStub : IEntitlementService {
        public Task<EntitlementDto> ResolveAsync(Guid organizationId) => Task.FromResult(new EntitlementDto(organizationId, "official", ["relatorios", "certificados"], new Dictionary<string, int>()));
        public Task<bool> CanUseAsync(Guid organizationId, string moduleCode) => Task.FromResult(true);
    }

    private sealed class PermissionStub(bool allow) : IPermissionService {
        public Task<bool> HasPermissionAsync(Guid userId, string permissionCode, Guid? organizationId = null) => Task.FromResult(allow);
    }

    private sealed class DocumentServiceStub : IValoraDocumentService {
        public Task<GeneratedDocument> GenerateAsync(DocumentRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GeneratedDocument(Guid.NewGuid(), request.OrganizationId, request.DiagnosisId, request.Format,
                "file.pdf", "application/pdf", [1, 2, 3], "trc", DateTimeOffset.UtcNow));
        public Task<GeneratedDocument?> OpenForDownloadAsync(Guid organizationId, Guid documentId, Guid? userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<GeneratedDocument?>(null);
    }

    private sealed class MemoryStore : IDocumentStore {
        public Task SaveAsync(GeneratedDocument document, Guid? generatedBy, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<GeneratedDocument?> FindAsync(Guid organizationId, Guid documentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<GeneratedDocument?>(null);
    }

    private sealed class AuditSpy : IExportAuditService {
        public Task RecordAsync(Guid organizationId, Guid? userId, string action, string resourceType, string resourceId, bool succeeded, string? detail = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
