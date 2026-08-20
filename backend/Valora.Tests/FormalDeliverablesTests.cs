using System.IO.Compression;
using System.Text.Json;
using Valora.Application.FormalDeliverables;

namespace Valora.Tests;

public sealed class FormalDeliverablesTests
{
    private static DiagnosisDocumentSnapshot Snapshot(bool completed = true) => new(
        Guid.NewGuid(), "Organização Exemplo", Guid.NewGuid(), "Diagnóstico de Cultura",
        completed ? DateTimeOffset.UtcNow : default, 78.4m, "Estruturado", "Valora Insight", "3.2",
        "A organização demonstra práticas consistentes e oportunidades objetivas de evolução.",
        "A governança é a principal alavanca para sustentar o avanço observado.",
        [new("Governança", 81m, "Práticas institucionalizadas")],
        [new("Governança", "Ritos decisórios documentados", "Diagnóstico consolidado")],
        ["Dependência de atores-chave"], ["Automatizar indicadores"], ["Clareza estratégica"], ["Baixa cadência de revisão"],
        ["Instituir revisão trimestral"], [new("Alta", "Criar fórum de indicadores", "Diretoria", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)))], true);

    [Fact] public void Pdf_IsARealNonEmptyPdf()
    {
        var file = new ExecutiveReportExportService().Render(Snapshot(), DeliverableFormat.Pdf, DateTimeOffset.UtcNow);
        Assert.True(file.Content.Length > 500); Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(file.Content, 0, 4));
        Assert.Equal("application/pdf", file.ContentType);
    }

    [Fact] public void Xlsx_HasAllRequiredWorksheets()
    {
        var file = new ExecutiveReportExportService().Render(Snapshot(), DeliverableFormat.Xlsx, DateTimeOffset.UtcNow);
        using var zip = new ZipArchive(new MemoryStream(file.Content));
        Assert.Equal(9, zip.Entries.Count(x => x.FullName.StartsWith("xl/worksheets/sheet", StringComparison.Ordinal)));
        Assert.Contains(zip.Entries, x => x.FullName == "xl/workbook.xml");
    }

    [Fact] public void Json_UsesSafeConsolidatedContract()
    {
        var file = new ExecutiveReportExportService().Render(Snapshot(), DeliverableFormat.Json, DateTimeOffset.UtcNow);
        using var json = JsonDocument.Parse(file.Content);
        Assert.Equal("3.2", json.RootElement.GetProperty("methodologyVersion").GetString());
        Assert.False(json.RootElement.TryGetProperty("respondents", out _));
        Assert.True(json.RootElement.GetProperty("evidenceItems").GetArrayLength() > 0);
    }

    [Fact] public async Task IncompleteDiagnosis_IsRejectedAndAudited()
    {
        var snapshot = Snapshot(false); var audit = new AuditSpy();
        var service = new ValoraDocumentService(new SnapshotProvider(snapshot), new AllowPolicy(), new MemoryStore(), new ExecutiveReportExportService(), audit);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateAsync(new(snapshot.OrganizationId, snapshot.DiagnosisId, DeliverableFormat.Pdf, null)));
        Assert.Contains(audit.Actions, x => x == "deliverable.generation_failed");
    }

    private sealed class SnapshotProvider(DiagnosisDocumentSnapshot value) : IDiagnosisDocumentSnapshotProvider { public Task<DiagnosisDocumentSnapshot?> LoadAsync(Guid o, Guid d, CancellationToken c = default) => Task.FromResult<DiagnosisDocumentSnapshot?>(value); }
    private sealed class AllowPolicy : IDocumentAccessPolicy { public Task EnsureCanGenerateAsync(Guid o, Guid? u, DeliverableFormat f, CancellationToken c = default) => Task.CompletedTask; }
    private sealed class MemoryStore : IDocumentStore { public Task SaveAsync(GeneratedDocument d, Guid? u, CancellationToken c = default) => Task.CompletedTask; public Task<GeneratedDocument?> FindAsync(Guid o, Guid d, CancellationToken c = default) => Task.FromResult<GeneratedDocument?>(null); }
    private sealed class AuditSpy : IExportAuditService { public List<string> Actions { get; } = []; public Task RecordAsync(Guid o, Guid? u, string a, string t, string r, bool s, string? d = null, CancellationToken c = default) { Actions.Add(a); return Task.CompletedTask; } }
}
