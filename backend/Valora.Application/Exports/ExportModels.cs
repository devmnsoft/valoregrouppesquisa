namespace Valora.Application.Contracts;

public sealed record ExportWorkItem(Guid Id, Guid OrganizationId, string Entity, string Format,
    string FilterJson, int Attempts, int MaxAttempts, string CorrelationId);

public sealed record ExportDataSet(IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows);

public sealed record GeneratedExport(Guid JobId, Guid OrganizationId, string FileName, string MimeType,
    byte[] Content, string ChecksumSha256, DateTimeOffset ExpiresAt);
