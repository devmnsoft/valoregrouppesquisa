using System.Security.Cryptography;
using System.Text;

namespace Valora.Application.FormalDeliverables;

public sealed class SecureShareLinkService(IShareLinkRepository repository, IExportAuditService audit) : ISecureShareLinkService {
    public Task<CreatedShareLink> CreateAsync(Guid organizationId, Guid diagnosisId, Guid? userId, TimeSpan lifetime, bool allowDownload, CancellationToken cancellationToken = default) =>
        CreateCoreAsync(organizationId, diagnosisId, null, diagnosisId, userId, lifetime, allowDownload, cancellationToken);

    public Task<CreatedShareLink> CreateForDeliverableAsync(Guid organizationId, Guid deliverableId, Guid resultId, Guid? userId, TimeSpan lifetime, bool allowDownload, CancellationToken cancellationToken = default) =>
        CreateCoreAsync(organizationId, resultId, deliverableId, resultId, userId, lifetime, allowDownload, cancellationToken);

    private async Task<CreatedShareLink> CreateCoreAsync(Guid organizationId, Guid diagnosisId, Guid? deliverableId, Guid? resultId, Guid? userId, TimeSpan lifetime, bool allowDownload, CancellationToken cancellationToken) {
        if (lifetime <= TimeSpan.Zero || lifetime > TimeSpan.FromDays(90))
            throw new ArgumentOutOfRangeException(nameof(lifetime), "A validade deve estar entre um instante e 90 dias.");
        var token = Base64Url(RandomNumberGenerator.GetBytes(32));
        var id = Guid.NewGuid();
        // Opaque route segment is the token; only its SHA-256 digest and public_slug=id are persisted.
        var link = new ShareLink(id, organizationId, diagnosisId, Hash(token), id.ToString("N"), DateTimeOffset.UtcNow.Add(lifetime), allowDownload,
            DeliverableId: deliverableId, ResultId: resultId);
        await repository.SaveAsync(link, userId, cancellationToken);
        await audit.RecordAsync(organizationId, userId, "share_link.created", deliverableId.HasValue ? "formal_deliverable" : "diagnosis",
            (deliverableId ?? diagnosisId).ToString(), true, null, cancellationToken);
        return new CreatedShareLink(link.Id, token, token, link.ExpiresAt, link.AllowDownload);
    }

    public async Task<ShareLink?> ResolveAsync(string token, bool downloadRequested, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(token) || token.Length < 40) return null;
        var link = await repository.FindByHashAsync(Hash(token), cancellationToken);
        if (link is null || link.RevokedAt.HasValue || link.ExpiresAt <= DateTimeOffset.UtcNow ||
            (link.MaxAccessCount.HasValue && link.AccessCount >= link.MaxAccessCount.Value)) return null;
        if (downloadRequested && !link.AllowDownload) return null;
        await repository.RegisterAccessAsync(link.Id, downloadRequested, cancellationToken);
        await audit.RecordAsync(link.OrganizationId, null, "share_link.accessed", "diagnosis", link.DiagnosisId.ToString(), true, null, cancellationToken);
        return link;
    }

    public async Task<bool> RevokeAsync(Guid organizationId, Guid linkId, Guid? userId, CancellationToken cancellationToken = default) {
        var revoked = await repository.RevokeAsync(organizationId, linkId, cancellationToken);
        await audit.RecordAsync(organizationId, userId, "share_link.revoked", "share_link", linkId.ToString(), revoked, null, cancellationToken);
        return revoked;
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
