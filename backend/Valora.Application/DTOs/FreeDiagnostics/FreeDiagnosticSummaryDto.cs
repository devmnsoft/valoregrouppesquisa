namespace Valora.Application.DTOs.FreeDiagnostics;

public sealed record FreeDiagnosticSummaryDto(int TotalResponses,int TodayResponses,int MonthResponses,int EmailsSent,int EmailsPending,int EmailsProcessing,int EmailsFailed,int CertificatesGenerated,int SharedLinks);
