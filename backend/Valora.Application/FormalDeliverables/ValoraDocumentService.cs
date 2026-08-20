namespace Valora.Application.FormalDeliverables;

public sealed class ValoraDocumentService(
    IDiagnosisDocumentSnapshotProvider snapshots,
    IDocumentAccessPolicy access,
    IDocumentStore store,
    IExecutiveReportExportService exporter,
    IExportAuditService audit) : IValoraDocumentService
{
    public async Task<GeneratedDocument> GenerateAsync(DocumentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await access.EnsureCanGenerateAsync(request.OrganizationId, request.UserId, request.Format, cancellationToken);
            var snapshot = await snapshots.LoadAsync(request.OrganizationId, request.DiagnosisId, cancellationToken)
                ?? throw new KeyNotFoundException("Diagnóstico não encontrado para esta organização.");
            if (snapshot.CompletedAt == default) throw new InvalidOperationException("O diagnóstico precisa estar concluído antes da emissão.");
            var document = exporter.Render(snapshot, request.Format, DateTimeOffset.UtcNow);
            if (document.Content.Length == 0) throw new InvalidOperationException("O gerador produziu um documento vazio.");
            await store.SaveAsync(document, request.UserId, cancellationToken);
            await audit.RecordAsync(request.OrganizationId, request.UserId, "deliverable.generated", request.Format.ToString(), document.Id.ToString(), true, document.TraceCode, cancellationToken);
            return document;
        }
        catch (Exception ex)
        {
            await audit.RecordAsync(request.OrganizationId, request.UserId, "deliverable.generation_failed", request.Format.ToString(), request.DiagnosisId.ToString(), false, ex.Message, cancellationToken);
            throw;
        }
    }

    public async Task<GeneratedDocument?> OpenForDownloadAsync(Guid organizationId, Guid documentId, Guid? userId, CancellationToken cancellationToken = default)
    {
        await access.EnsureCanGenerateAsync(organizationId, userId, DeliverableFormat.Pdf, cancellationToken);
        var document = await store.FindAsync(organizationId, documentId, cancellationToken);
        await audit.RecordAsync(organizationId, userId, "deliverable.downloaded", "document", documentId.ToString(), document is not null, null, cancellationToken);
        return document;
    }
}
