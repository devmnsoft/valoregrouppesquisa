namespace Valora.Application.FormalDeliverables;

/// <summary>Explicit application boundary used by MVC/API endpoints and background jobs.</summary>
public sealed class ExecutiveReportGenerationService(IValoraDocumentService documents)
{
    public Task<GeneratedDocument> GenerateAsync(Guid organizationId, Guid resultId, Guid userId, DeliverableFormat format = DeliverableFormat.Pdf, CancellationToken ct = default) =>
        documents.GenerateAsync(new(organizationId, resultId, format, userId), ct);
}

public sealed class CertificateGenerationService(IValoraDocumentService documents)
{
    public Task<GeneratedDocument> GenerateAsync(Guid organizationId, Guid resultId, Guid userId, CancellationToken ct = default) =>
        documents.GenerateAsync(new(organizationId, resultId, DeliverableFormat.CertificatePdf, userId), ct);
}

public sealed class PublicResultPortalService(ISecureShareLinkService links, IDiagnosisDocumentSnapshotProvider snapshots)
{
    public async Task<DiagnosisDocumentSnapshot?> OpenAsync(string token, CancellationToken ct = default)
    {
        var link = await links.ResolveAsync(token, false, ct);
        return link is null ? null : await snapshots.LoadAsync(link.OrganizationId, link.DiagnosisId, ct);
    }
}

public sealed class DeliverableAuditService(IExportAuditService audit)
{
    public Task RecordAsync(Guid organizationId, Guid? actorId, string action, Guid resourceId, bool succeeded, CancellationToken ct = default) =>
        audit.RecordAsync(organizationId, actorId, action, "formal_deliverable", resourceId.ToString(), succeeded, cancellationToken: ct);
}

public sealed class GenerateExecutiveReportUseCase(ExecutiveReportGenerationService service)
{
    public Task<GeneratedDocument> ExecuteAsync(Guid organizationId, Guid resultId, Guid userId, DeliverableFormat format, CancellationToken ct = default) => service.GenerateAsync(organizationId, resultId, userId, format, ct);
}
public sealed class GenerateCertificateUseCase(CertificateGenerationService service)
{
    public Task<GeneratedDocument> ExecuteAsync(Guid organizationId, Guid resultId, Guid userId, CancellationToken ct = default) => service.GenerateAsync(organizationId, resultId, userId, ct);
}
public sealed class CreateSecureShareLinkUseCase(ISecureShareLinkService service)
{
    public Task<CreatedShareLink> ExecuteAsync(Guid organizationId, Guid resultId, Guid userId, TimeSpan lifetime, bool allowDownload, CancellationToken ct = default) => service.CreateAsync(organizationId, resultId, userId, lifetime, allowDownload, ct);
}
public sealed class ValidateSecureShareLinkUseCase(ISecureShareLinkService service)
{
    public Task<ShareLink?> ExecuteAsync(string token, bool download, CancellationToken ct = default) => service.ResolveAsync(token, download, ct);
}
public sealed class DownloadCertificateUseCase(IValoraDocumentService documents)
{
    public Task<GeneratedDocument?> ExecuteAsync(Guid organizationId, Guid documentId, Guid? userId, CancellationToken ct = default) => documents.OpenForDownloadAsync(organizationId, documentId, userId, ct);
}
public sealed class DownloadReportUseCase(IValoraDocumentService documents)
{
    public Task<GeneratedDocument?> ExecuteAsync(Guid organizationId, Guid documentId, Guid? userId, CancellationToken ct = default) => documents.OpenForDownloadAsync(organizationId, documentId, userId, ct);
}
public sealed class RevokeShareLinkUseCase(ISecureShareLinkService service)
{
    public Task<bool> ExecuteAsync(Guid organizationId, Guid linkId, Guid? userId, CancellationToken ct = default) => service.RevokeAsync(organizationId, linkId, userId, ct);
}
public sealed class RegisterPublicResultAccessUseCase(ISecureShareLinkService service)
{
    public Task<ShareLink?> ExecuteAsync(string token, CancellationToken ct = default) => service.ResolveAsync(token, false, ct);
}
