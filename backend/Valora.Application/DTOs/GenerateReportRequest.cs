namespace Valora.Application.DTOs;

public sealed record GenerateReportRequest(string Format = "html", Guid? ReportDefinitionId = null);
