using System.Security.Cryptography;
using System.Text;
using Valora.Application.Exceptions;

namespace Valora.Application.FormalDeliverables;

public sealed class SecureShareLinkService(IShareLinkRepository repository, IExportAuditService audit) : ISecureShareLinkService {
    public Task<CreatedShareLink> CreateAsync(Guid organizationId, Guid diagnosisId, Guid? userId, TimeSpan lifetime, bool allowDownload, CancellationToken cancellationToken = default) =>
        CreateCoreAsync(organizationId, diagnosisId, null, diagnosisId, userId, lifetime, allowDownload, null, null, cancellationToken);

    public Task<CreatedShareLink> CreateForDeliverableAsync(Guid organizationId, Guid deliverableId, Guid resultId, Guid? userId, TimeSpan lifetime, bool allowDownload, string? commandId = null, string? requestHash = null, CancellationToken cancellationToken = default) =>
        CreateCoreAsync(organizationId, resultId, deliverableId, resultId, userId, lifetime, allowDownload, commandId, requestHash, cancellationToken);

    private async Task<CreatedShareLink> CreateCoreAsync(Guid organizationId, Guid diagnosisId, Guid? deliverableId, Guid? resultId, Guid? userId, TimeSpan lifetime, bool allowDownload, string? commandId, string? requestHash, CancellationToken cancellationToken) {
        if (lifetime <= TimeSpan.Zero || lifetime > TimeSpan.FromDays(90))
            throw new ArgumentOutOfRangeException(nameof(lifetime), "A validade deve estar entre um instante e 90 dias.");
        commandId = string.IsNullOrWhiteSpace(commandId) ? null : commandId.Trim();
        if (commandId is not null) {
            if (commandId.Length > 80)
                throw new ValidationAppException("A chave da operação deve ter até 80 caracteres.");
            if (string.IsNullOrWhiteSpace(requestHash))
                throw new ArgumentException("O hash da intenção é obrigatório para criação idempotente.", nameof(requestHash));
            var existing = await repository.FindByCommandIdAsync(organizationId, commandId, cancellationToken);
            if (existing is not null) {
                return await ReplayAsync(existing, requestHash, userId, cancellationToken);
            }
        }
        var token = Base64Url(RandomNumberGenerator.GetBytes(32));
        var id = Guid.NewGuid();
        // Opaque route segment is the token; only its SHA-256 digest and public_slug=id are persisted.
        var link = new ShareLink(id, organizationId, diagnosisId, Hash(token), id.ToString("N"), DateTimeOffset.UtcNow.Add(lifetime), allowDownload,
            DeliverableId: deliverableId, ResultId: resultId, CommandId: commandId, RequestHash: requestHash);
        if (!await repository.TrySaveAsync(link, userId, cancellationToken)) {
            var existing = commandId is null ? null : await repository.FindByCommandIdAsync(organizationId, commandId, cancellationToken);
            if (existing is null)
                throw new ConcurrencyConflictException("O link seguro não foi criado porque outra operação concorrente reutilizou a mesma chave.");
            return await ReplayAsync(existing, requestHash, userId, cancellationToken);
        }
        await audit.RecordAsync(organizationId, userId, "share_link.created", deliverableId.HasValue ? "formal_deliverable" : "diagnosis",
            (deliverableId ?? diagnosisId).ToString(), true, null, cancellationToken);
        return new CreatedShareLink(link.Id, token, token, link.ExpiresAt, link.AllowDownload);
    }

    private async Task<CreatedShareLink> ReplayAsync(ShareLink existing, string? requestHash, Guid? userId, CancellationToken cancellationToken) {
        if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            throw new ConcurrencyConflictException("A chave da operação de compartilhamento já foi usada com outro conteúdo.");
        await audit.RecordAsync(existing.OrganizationId, userId, "share_link.replayed", existing.DeliverableId.HasValue ? "formal_deliverable" : "diagnosis",
            (existing.DeliverableId ?? existing.DiagnosisId).ToString(), true, existing.Id.ToString("N"), cancellationToken);
        return new CreatedShareLink(existing.Id, "", existing.PublicSlug, existing.ExpiresAt, existing.AllowDownload,
            TokenAvailable: false,
            ReplayMessage: "Este comando já criou um link seguro. O token original não é recuperável; revogue e emita um novo link se precisar exibi-lo novamente.");
    }

    public async Task<ShareLink?> ResolveAsync(string token, bool downloadRequested, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(token) || token.Length < 40) return null;
        var link = await repository.FindByHashAsync(Hash(token), cancellationToken);
        if (link is null || link.RevokedAt.HasValue || link.ExpiresAt <= DateTimeOffset.UtcNow ||
            (link.MaxAccessCount.HasValue && link.AccessCount >= link.MaxAccessCount.Value)) return null;
        if (downloadRequested && !link.AllowDownload) return null;
        if (!await repository.RegisterAccessAsync(link.Id, downloadRequested, cancellationToken))
            return null;
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
