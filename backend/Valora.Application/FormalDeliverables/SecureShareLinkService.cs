using System.Security.Cryptography;
using System.Text;

namespace Valora.Application.FormalDeliverables;

public sealed class SecureShareLinkService(IShareLinkRepository repository, IExportAuditService audit) : ISecureShareLinkService
{
    public async Task<CreatedShareLink> CreateAsync(Guid organizationId, Guid diagnosisId, Guid? userId, TimeSpan lifetime, bool allowDownload, CancellationToken cancellationToken = default)
    {
        if (lifetime <= TimeSpan.Zero || lifetime > TimeSpan.FromDays(90))
            throw new ArgumentOutOfRangeException(nameof(lifetime), "A validade deve estar entre um instante e 90 dias.");
        var token = Base64Url(RandomNumberGenerator.GetBytes(32));
        // The opaque route segment is the token itself. Only its SHA-256 digest is persisted.
        var link = new ShareLink(Guid.NewGuid(), organizationId, diagnosisId, Hash(token), token, DateTimeOffset.UtcNow.Add(lifetime), allowDownload);
        await repository.SaveAsync(link, userId, cancellationToken);
        await audit.RecordAsync(organizationId, userId, "share_link.created", "diagnosis", diagnosisId.ToString(), true, null, cancellationToken);
        return new CreatedShareLink(link.Id, token, token, link.ExpiresAt, link.AllowDownload);
    }

    public async Task<ShareLink?> ResolveAsync(string token, bool downloadRequested, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length < 40) return null;
        var link = await repository.FindByHashAsync(Hash(token), cancellationToken);
        if (link is null || link.RevokedAt.HasValue || link.ExpiresAt <= DateTimeOffset.UtcNow ||
            (link.MaxAccessCount.HasValue && link.AccessCount >= link.MaxAccessCount.Value)) return null;
        if (downloadRequested && !link.AllowDownload) return null;
        await repository.RegisterAccessAsync(link.Id, downloadRequested, cancellationToken);
        await audit.RecordAsync(link.OrganizationId, null, "share_link.accessed", "diagnosis", link.DiagnosisId.ToString(), true, null, cancellationToken);
        return link;
    }

    public async Task<bool> RevokeAsync(Guid organizationId, Guid linkId, Guid? userId, CancellationToken cancellationToken = default)
    {
        var revoked = await repository.RevokeAsync(organizationId, linkId, cancellationToken);
        await audit.RecordAsync(organizationId, userId, "share_link.revoked", "share_link", linkId.ToString(), revoked, null, cancellationToken);
        return revoked;
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
