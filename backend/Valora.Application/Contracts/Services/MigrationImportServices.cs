using Valora.Application.DTOs;
namespace Valora.Application.Contracts;
public sealed record LegacySourceDocument(string Collection,string LegacyId,string TargetEntity,string MaskedJson,string NormalizedMaskedJson,IReadOnlyList<string> UnmappedFields,IReadOnlyList<string> SensitiveFields);
public sealed record LegacySourceReadResult(string SourceType,string SourceName,string Sha256,IReadOnlyList<LegacySourceDocument> Documents);
public interface ILegacyImportService { Task<MigrationValidationReportDto> DryRunAsync(MigrationDryRunRequest request,CancellationToken ct=default); }
public interface ILegacySourceReader { bool CanRead(string sourceType); Task<LegacySourceReadResult> ReadAsync(MigrationUploadRequest request,CancellationToken ct=default); }
public interface IFirestoreExportReader:ILegacySourceReader { }
public interface ILocalStorageExportReader:ILegacySourceReader { }
public interface IManualJsonReader:ILegacySourceReader { }
public interface ILegacyMappingService { string MapCollectionToTarget(string collection); IReadOnlyList<string> GetUnmappedFields(string collection,IEnumerable<string> fields); }
public interface ILegacyDataNormalizer { string? NormalizeEmail(string? value); string? NormalizeDocument(string? value); string NormalizeStatus(string? value); string NormalizeRole(string? value); string NormalizeModule(string? value); DateTime? NormalizeDate(object? value); string MaskSensitiveJson(string? json); }
public interface IMigrationDryRunService { Task<MigrationValidationReportDto> ExecuteAsync(MigrationDryRunRequest request,CancellationToken ct=default); }
public interface IMigrationApplyService { Task<MigrationReconciliationReportDto> ApplyAsync(MigrationApplyRequest request,CancellationToken ct=default); }
public interface IMigrationReconciliationService { Task<MigrationReconciliationReportDto> ReconcileAsync(Guid batchId,CancellationToken ct=default); }
public interface IMigrationRollbackService { Task<MigrationReconciliationReportDto> RollbackAsync(MigrationRollbackRequest request,CancellationToken ct=default); Task<IReadOnlyList<MigrationRollbackItemDto>> GetReportAsync(Guid batchId,CancellationToken ct=default); }
public interface ICutoverReadinessService { Task<CutoverReadinessDto> GetAsync(Guid batchId,CancellationToken ct=default); }
public interface IMigrationReportService { Task<MigrationValidationReportDto> GetDryRunReportAsync(Guid batchId,CancellationToken ct=default); Task<MigrationReconciliationReportDto> GetReconciliationAsync(Guid batchId,CancellationToken ct=default); }
