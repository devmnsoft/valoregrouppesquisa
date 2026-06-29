using Valora.Application.DTOs.FreeDiagnostics;
namespace Valora.Application.Contracts;
public interface IFreeDiagnosticsRepository
{
    Task<IReadOnlyList<FreeDiagnosticListItemDto>> ListAsync(FreeDiagnosticFilter filter, Guid? organizationId);
    Task<FreeDiagnosticSummaryDto> SummaryAsync(FreeDiagnosticFilter filter, Guid? organizationId);
    Task<FreeDiagnosticDetailDto?> DetailAsync(Guid responseId, Guid? organizationId);
    Task<int> CountManualResendsLast24hAsync(Guid responseId);
    Task CreateResendEmailJobAsync(Guid responseId, Guid? actorUserId, string justification);
    Task RegenerateCertificateMetadataAsync(Guid responseId, Guid? actorUserId, string? layoutVersion);
    Task MarkReviewedAsync(Guid responseId, Guid? actorUserId, string reviewNote);
    Task AuditAsync(Guid? organizationId, Guid responseId, Guid? actorUserId, string eventType, string actorType, string metadataJson);
}
