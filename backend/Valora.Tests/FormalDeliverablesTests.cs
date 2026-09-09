using System.IO.Compression;
using System.Text.Json;
using Valora.Application.FormalDeliverables;

namespace Valora.Tests;

public sealed class FormalDeliverablesTests
{
 
     

    private sealed class SnapshotProvider(DiagnosisDocumentSnapshot value) : IDiagnosisDocumentSnapshotProvider { public Task<DiagnosisDocumentSnapshot?> LoadAsync(Guid o, Guid d, CancellationToken c = default) => Task.FromResult<DiagnosisDocumentSnapshot?>(value); }
    private sealed class AllowPolicy : IDocumentAccessPolicy { public Task EnsureCanGenerateAsync(Guid o, Guid? u, DeliverableFormat f, CancellationToken c = default) => Task.CompletedTask; }
    private sealed class MemoryStore : IDocumentStore { public Task SaveAsync(GeneratedDocument d, Guid? u, CancellationToken c = default) => Task.CompletedTask; public Task<GeneratedDocument?> FindAsync(Guid o, Guid d, CancellationToken c = default) => Task.FromResult<GeneratedDocument?>(null); }
    private sealed class AuditSpy : IExportAuditService { public List<string> Actions { get; } = []; public Task RecordAsync(Guid o, Guid? u, string a, string t, string r, bool s, string? d = null, CancellationToken c = default) { Actions.Add(a); return Task.CompletedTask; } }
}
