namespace Valora.Application.DTOs.FreeDiagnostics;

public sealed record FreeDiagnosticFilter(DateTime? StartDate,DateTime? EndDate,string? Name,string? Email,string? EmailStatus,string? MaturityLevel,string? CertificateStatus,int Page=1,int PageSize=50);
