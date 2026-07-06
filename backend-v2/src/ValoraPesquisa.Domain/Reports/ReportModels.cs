namespace ValoraPesquisa.Domain.Reports;
public enum ReportFormat { Csv, Xlsx, PdfHtml, Json }
public enum ReportExecutionStatus { Pending, Running, Completed, Failed, Expired }
public sealed record ReportDefinition(Guid Id, Guid TenantId, string Name, string QueryKey, string? Description, bool Enabled);
public sealed record ReportFilter(Guid Id, Guid ReportDefinitionId, string Name, string Type, bool Required);
public sealed record ReportExecution(Guid Id, Guid ReportDefinitionId, Guid TenantId, ReportExecutionStatus Status, DateTimeOffset StartedAt, DateTimeOffset? FinishedAt);
public sealed record ReportExport(Guid Id, Guid ExecutionId, ReportFormat Format, string StorageKey, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);
public sealed record ReportSchedule(Guid Id, Guid ReportDefinitionId, string CronExpression, bool Enabled);
