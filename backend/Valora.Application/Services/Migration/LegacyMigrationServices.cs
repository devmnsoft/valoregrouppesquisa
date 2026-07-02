using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

public sealed class LegacyDataNormalizer : ILegacyDataNormalizer
{
    static readonly Regex Sensitive = new("(?i)(password|senha|token|secret|smtp|connection|string|hash|refresh)");
    public string? NormalizeEmail(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
    public string? NormalizeDocument(string? value) => string.IsNullOrWhiteSpace(value) ? null : Regex.Replace(value, "\\D", "");
    public string NormalizeStatus(string? value) => (value ?? "active").Trim().ToLowerInvariant() switch { "ativo" or "active" or "published" => "active", "inativo" or "inactive" or "disabled" => "inactive", "draft" or "rascunho" => "draft", "completed" or "concluido" => "completed", _ => "pending" };
    public string NormalizeRole(string? value) => (value ?? "participant").Trim().ToLowerInvariant() switch { "admin" or "admin_valora" or "superadmin" => "admin_valora", "gestor" or "manager" => "manager", _ => "participant" };
    public string NormalizeModule(string? value) => Regex.Replace((value ?? "core").Trim().ToLowerInvariant(), "[^a-z0-9_-]", "-");
    public DateTime? NormalizeDate(object? value) => value is null ? null : DateTime.TryParse(value.ToString(), out var dt) ? DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToUniversalTime() : null;
    public string MaskSensitiveJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return "{}";
        using var doc = JsonDocument.Parse(json); return Mask(doc.RootElement).GetRawText();
    }
    private JsonElement Mask(JsonElement e) => JsonSerializer.SerializeToElement(e.ValueKind switch
    {
        JsonValueKind.Object => e.EnumerateObject().ToDictionary(p => p.Name, p => Sensitive.IsMatch(p.Name) ? "***MASKED***" : (object)Mask(p.Value)),
        JsonValueKind.Array => e.EnumerateArray().Select(x => (object)Mask(x)).ToArray(),
        _ => e.ToString() ?? string.Empty
    });
}

public sealed class LegacyMappingService : ILegacyMappingService
{
    static readonly Dictionary<string,string> Map = new(StringComparer.OrdinalIgnoreCase){{"companies","organizations"},{"organizations","organizations"},{"companyProfiles","organizations"},{"users","users"},{"authUsers","users"},{"companyUsers","users"},{"participants","survey_participants"},{"plans","plans"},{"modules","modules"},{"companyModules","organization_modules"},{"subscription","subscriptions"},{"forms","forms"},{"questions","questions"},{"dimensions","form_dimensions"},{"options","question_options"},{"surveys","surveys"},{"publicLinks","survey_links"},{"surveyLinks","survey_links"},{"invites","survey_invites"},{"responses","responses"},{"answers","response_answers"},{"results","result_scores"},{"scores","dimension_scores"},{"certificates","certificates"},{"emailJobs","email_jobs"},{"communications","communications"},{"outbox","email_jobs"},{"auditLogs","audit_logs"},{"logs","audit_logs"},{"events","audit_logs"},{"consents","lgpd_consents"},{"privacyRequests","privacy_requests"}};
    public string MapCollectionToTarget(string collection) => Map.GetValueOrDefault(collection, "manual_review");
    public IReadOnlyList<string> GetUnmappedFields(string collection, IEnumerable<string> fields) => Map.ContainsKey(collection) ? Array.Empty<string>() : fields.ToArray();
}

public abstract class JsonLegacySourceReader(ILegacyMappingService mapping, ILegacyDataNormalizer normalizer) : ILegacySourceReader
{
    public abstract bool CanRead(string sourceType);
    public Task<LegacySourceReadResult> ReadAsync(MigrationUploadRequest request, CancellationToken ct=default)
    {
        try { using var doc = JsonDocument.Parse(request.PayloadJson); var list = new List<LegacySourceDocument>(); ReadRoot(doc.RootElement, list); var sha = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.PayloadJson))).ToLowerInvariant(); return Task.FromResult(new LegacySourceReadResult(request.SourceType, request.SourceName, sha, list)); }
        catch (JsonException ex) { throw new InvalidOperationException($"JSON inválido para importação: {ex.Message}"); }
    }
    void ReadRoot(JsonElement root, List<LegacySourceDocument> list)
    {
        var source = root.TryGetProperty("collections", out var c) ? c : root.TryGetProperty("data", out var d) ? d : root;
        foreach (var col in source.EnumerateObject())
        {
            if (col.Value.ValueKind == JsonValueKind.Array) { var i=0; foreach (var item in col.Value.EnumerateArray()) Add(col.Name, Id(item) ?? (++i).ToString(), item, list); }
            else if (col.Value.ValueKind == JsonValueKind.Object) { foreach (var item in col.Value.EnumerateObject()) Add(col.Name, item.Name, item.Value, list); }
        }
    }
    static string? Id(JsonElement e) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty("id", out var id) ? id.ToString() : null;
    void Add(string collection,string id,JsonElement item,List<LegacySourceDocument> list)
    { var raw=item.GetRawText(); var fields=item.ValueKind==JsonValueKind.Object?item.EnumerateObject().Select(p=>p.Name):Array.Empty<string>(); var sensitive=fields.Where(f=>Regex.IsMatch(f,"(?i)(password|senha|token|secret|hash|smtp|connection)")).ToArray(); list.Add(new(collection,id,mapping.MapCollectionToTarget(collection),normalizer.MaskSensitiveJson(raw),normalizer.MaskSensitiveJson(raw),mapping.GetUnmappedFields(collection,fields),sensitive)); }
}
public sealed class FirestoreExportReader(ILegacyMappingService m, ILegacyDataNormalizer n) : JsonLegacySourceReader(m,n), IFirestoreExportReader { public override bool CanRead(string sourceType)=>sourceType.Equals("firestore",StringComparison.OrdinalIgnoreCase); }
public sealed class LocalStorageExportReader(ILegacyMappingService m, ILegacyDataNormalizer n) : JsonLegacySourceReader(m,n), ILocalStorageExportReader { public override bool CanRead(string sourceType)=>sourceType.Equals("localStorage",StringComparison.OrdinalIgnoreCase)||sourceType.Equals("local",StringComparison.OrdinalIgnoreCase); }
public sealed class ManualJsonReader(ILegacyMappingService m, ILegacyDataNormalizer n) : JsonLegacySourceReader(m,n), IManualJsonReader { public override bool CanRead(string sourceType)=>sourceType.Equals("manual",StringComparison.OrdinalIgnoreCase)||sourceType.Equals("json",StringComparison.OrdinalIgnoreCase); }

public sealed class MigrationDryRunService(IEnumerable<ILegacySourceReader> readers, IMigrationRecordRepository records, IMigrationConflictRepository conflicts) : IMigrationDryRunService, ILegacyImportService
{
    public Task<MigrationValidationReportDto> DryRunAsync(MigrationDryRunRequest request,CancellationToken ct=default)=>ExecuteAsync(request,ct);
    public async Task<MigrationValidationReportDto> ExecuteAsync(MigrationDryRunRequest request, CancellationToken ct=default)
    {
        var recs=new List<MigrationRecordDto>(); var cons=new List<MigrationConflictDto>(); var unmapped=new HashSet<string>(); var sensitive=new HashSet<string>(); var total=0; var invalid=0;
        foreach(var src in request.Sources){var reader=readers.FirstOrDefault(r=>r.CanRead(src.SourceType))??throw new InvalidOperationException("Fonte de importação não suportada."); var data=await reader.ReadAsync(src,ct); foreach(var d in data.Documents){total++; foreach(var u in d.UnmappedFields) unmapped.Add($"{d.Collection}.{u}"); foreach(var s in d.SensitiveFields) sensitive.Add($"{d.Collection}.{s}"); var status=d.TargetEntity=="manual_review"?"invalid":"planned"; if(status=="invalid") invalid++; var dto=new MigrationRecordDto(Guid.NewGuid(),request.BatchId,null,d.Collection,d.LegacyId,d.TargetEntity,null,"insert",status,d.MaskedJson,d.NormalizedMaskedJson,status=="invalid"?"UNMAPPED_COLLECTION":null,status=="invalid"?"Coleção sem destino oficial claro.":null); recs.Add(dto); await records.AddAsync(dto,ct); if(status=="invalid"){var c=new MigrationConflictDto(Guid.NewGuid(),request.BatchId,d.Collection,d.LegacyId,d.TargetEntity,null,"unmapped_collection","blocking",d.MaskedJson,"{}",null,null,null); cons.Add(c); await conflicts.AddAsync(c,ct);}}}
        var sum=new MigrationSummaryDto(total,total-invalid,invalid,total-invalid,0,0,cons.Count,unmapped.ToArray(),sensitive.ToArray(),cons.Count>0?new[]{"Há conflitos bloqueantes antes do apply."}:Array.Empty<string>());
        return new MigrationValidationReportDto(request.BatchId, cons.Any(c=>c.Severity=="blocking")?"blocked":"dry_run_completed", sum, recs, cons);
    }
}
public sealed class MigrationApplyService(IMigrationBatchRepository batches, IMigrationConflictRepository conflicts, IMigrationMappingRepository mappings, IMigrationRecordRepository records, IMigrationRollbackRepository rollbacks) : IMigrationApplyService
{ public async Task<MigrationReconciliationReportDto> ApplyAsync(MigrationApplyRequest request,CancellationToken ct=default){ if(!request.ConfirmApply) throw new InvalidOperationException("Importação real exige confirmApply=true."); if(request.RequestedByRole!="admin_valora") throw new UnauthorizedAccessException("Apenas admin_valora pode aplicar importação nesta sprint."); var batch=await batches.GetAsync(request.BatchId,ct)??throw new InvalidOperationException("Batch não encontrado."); if(!batch.Status.Contains("dry_run") && batch.Status!="blocked") throw new InvalidOperationException("Apply exige dry-run anterior."); if(await conflicts.HasBlockingAsync(request.BatchId,ct)) throw new InvalidOperationException("Conflito bloqueante impede apply."); foreach(var r in await records.ListByBatchAsync(request.BatchId,ct)){var target=Guid.NewGuid(); await mappings.AddAsync(new MigrationMappingDto(Guid.NewGuid(),request.BatchId,r.LegacyCollection,r.LegacyId??r.Id.ToString(),r.TargetEntity,target,$"{r.LegacyCollection}:{r.LegacyId}"),ct); await rollbacks.AddAsync(new MigrationRollbackItemDto(Guid.NewGuid(),request.BatchId,r.TargetEntity,target,"insert",null,r.NormalizedMaskedJson,"planned",null),ct);} await batches.UpdateStatusAsync(request.BatchId,"applied","{\"sensitive\":\"masked\"}",ct); return new(request.BatchId,"ready_with_warnings",new Dictionary<string,int>(),new Dictionary<string,int>{{"records",(await records.ListByBatchAsync(request.BatchId,ct)).Count}},Array.Empty<string>()); } }
public sealed class MigrationReconciliationService(IMigrationRecordRepository records, IMigrationConflictRepository conflicts) : IMigrationReconciliationService { public async Task<MigrationReconciliationReportDto> ReconcileAsync(Guid batchId,CancellationToken ct=default){var r=await records.ListByBatchAsync(batchId,ct); var c=await conflicts.ListByBatchAsync(batchId,ct); return new(batchId,c.Any(x=>x.Severity=="blocking")?"blocked":"ready_with_warnings",r.GroupBy(x=>x.TargetEntity).ToDictionary(g=>g.Key,g=>g.Count()),r.Where(x=>x.Status!="invalid").GroupBy(x=>x.TargetEntity).ToDictionary(g=>g.Key,g=>g.Count()),c.Select(x=>$"{x.TargetEntity}:{x.ConflictType}").ToArray());}}
public sealed class MigrationRollbackService(IMigrationRollbackRepository repo) : IMigrationRollbackService { public Task<IReadOnlyList<MigrationRollbackItemDto>> GetReportAsync(Guid batchId,CancellationToken ct=default)=>repo.ListByBatchAsync(batchId,ct); public async Task<MigrationReconciliationReportDto> RollbackAsync(MigrationRollbackRequest request,CancellationToken ct=default){ if(!request.ConfirmRollback) throw new InvalidOperationException("Rollback exige confirmRollback=true."); if(request.RequestedByRole!="admin_valora") throw new UnauthorizedAccessException("Apenas admin_valora pode executar rollback."); var items=await repo.ListByBatchAsync(request.BatchId,ct); foreach(var i in items) await repo.MarkRolledBackAsync(i.Id,ct); return new(request.BatchId,"rolled_back",new Dictionary<string,int>(),new Dictionary<string,int>{{"rollback_items",items.Count}},Array.Empty<string>()); }}
public sealed class CutoverReadinessService(IMigrationConflictRepository conflicts) : ICutoverReadinessService { public async Task<CutoverReadinessDto> GetAsync(Guid batchId,CancellationToken ct=default){var c=await conflicts.ListByBatchAsync(batchId,ct); var blockers=c.Where(x=>x.Severity=="blocking").Select(x=>$"{x.TargetEntity}:{x.ConflictType}").ToArray(); return new(batchId,blockers.Length>0?"blocked":"ready_with_warnings",new[]{"Dry-run executado","Conciliação revisada","Rollback planejado","Auditoria ativa"},blockers,c.Where(x=>x.Severity!="blocking").Select(x=>x.ConflictType).ToArray(),"Executar janela manual, congelar legado, aplicar batch e validar amostras.","Executar rollback por batch e reativar legado preservado."); } }
public sealed class MigrationReportService(IMigrationRecordRepository records, IMigrationConflictRepository conflicts, IMigrationReconciliationService rec) : IMigrationReportService { public async Task<MigrationValidationReportDto> GetDryRunReportAsync(Guid batchId,CancellationToken ct=default){var r=await records.ListByBatchAsync(batchId,ct); var c=await conflicts.ListByBatchAsync(batchId,ct); var s=new MigrationSummaryDto(r.Count,r.Count(x=>x.Status!="invalid"),r.Count(x=>x.Status=="invalid"),r.Count(x=>x.Action=="insert"),r.Count(x=>x.Action=="update"),r.Count(x=>x.Action=="skip"),c.Count,Array.Empty<string>(),Array.Empty<string>(),Array.Empty<string>()); return new(batchId,c.Any(x=>x.Severity=="blocking")?"blocked":"dry_run_completed",s,r,c);} public Task<MigrationReconciliationReportDto> GetReconciliationAsync(Guid batchId,CancellationToken ct=default)=>rec.ReconcileAsync(batchId,ct); }
