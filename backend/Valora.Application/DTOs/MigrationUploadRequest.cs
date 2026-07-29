namespace Valora.Application.DTOs;

public sealed record MigrationUploadRequest(string SourceType, string SourceName, string FileName, string ContentType, string PayloadJson);
