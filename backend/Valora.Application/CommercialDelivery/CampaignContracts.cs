namespace Valora.Application.CommercialDelivery;

public sealed record CampaignRecipientRequest(string Email, bool HasConsent = true);
public sealed record CreateCampaignRequest(string Name, string Message, IReadOnlyList<CampaignRecipientRequest>? Recipients, string? Audience = null);
public sealed record CampaignRecipientDto(Guid Id, string MaskedRecipient, string Status, string? ErrorCode, DateTime CreatedAt);
public sealed record DiagnosticCampaignDto(Guid Id, Guid SurveyId, string Name, string Status, string? PublicUrl,
    string Message, int RecipientCount, int SentCount, int FailedCount, DateTime CreatedAt,
    IReadOnlyList<CampaignRecipientDto> Recipients);
public sealed record CampaignCommandResult(Guid CampaignId, string Status, string Message, string? PublicUrl);

public interface IDiagnosticCampaignRepository
{
    Task<DiagnosticCampaignDto?> GetAsync(Guid organizationId, Guid surveyId, CancellationToken ct);
    Task<DiagnosticCampaignDto?> CreateAsync(Guid organizationId, Guid surveyId, Guid userId, CreateCampaignRequest request, string correlationId, CancellationToken ct);
    Task<CampaignCommandResult?> SendAsync(Guid organizationId, Guid surveyId, Guid userId, bool emailConfigured, string correlationId, CancellationToken ct);
    Task<CampaignCommandResult?> CancelAsync(Guid organizationId, Guid surveyId, Guid userId, string correlationId, CancellationToken ct);
}

public interface IDiagnosticCampaignService
{
    Task<DiagnosticCampaignDto?> GetAsync(Guid organizationId, Guid surveyId, CancellationToken ct);
    Task<DiagnosticCampaignDto?> CreateAsync(Guid organizationId, Guid surveyId, Guid userId, CreateCampaignRequest request, string correlationId, CancellationToken ct);
    Task<CampaignCommandResult?> SendAsync(Guid organizationId, Guid surveyId, Guid userId, string correlationId, CancellationToken ct);
    Task<CampaignCommandResult?> CancelAsync(Guid organizationId, Guid surveyId, Guid userId, string correlationId, CancellationToken ct);
}
