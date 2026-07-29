namespace Valora.Application.Migration;

public sealed record LegacyImportResult(Guid BatchId, int Organizations, int Users, int Forms, int Surveys, int Responses, int Certificates, int Errors);
