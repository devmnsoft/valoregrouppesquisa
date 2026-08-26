using System.ComponentModel.DataAnnotations;

namespace Valora.Application.Experience;

public sealed record RespondentAccessToken(Guid Id, Guid OrganizationId, Guid DiagnosticId, Guid RespondentId,
    string Status, DateTimeOffset ExpiresAt, DateTimeOffset? FirstAccessAt, DateTimeOffset? CompletedAt);

public sealed record RespondentSession(Guid Id, Guid OrganizationId, Guid DiagnosticId, Guid RespondentId,
    Guid AccessTokenId, string Status, Guid? CurrentSectionId, decimal ProgressPercent, DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt);

public sealed record PublicResultView(Guid Id, Guid OrganizationId, Guid DiagnosticId, Guid ResultId, string Title,
    string Status, DateTimeOffset ExpiresAt, int AccessCount, bool AllowReport, bool AllowCertificate);

public sealed record IssuedPublicToken(Guid Id, string Token, DateTimeOffset ExpiresAt);

public sealed class SaveRespondentProgressRequest
{
    [Range(0, 100)] public decimal ProgressPercent { get; init; }
    public Guid? CurrentSectionId { get; init; }
    [Required, MinLength(2)] public string AnswersJson { get; init; } = "{}";
}

public interface IRespondentAccessTokenRepository
{
    Task<Guid> CreateAsync(Guid organizationId, Guid diagnosticId, Guid respondentId, string tokenHash, DateTimeOffset expiresAt, CancellationToken ct);
    Task<RespondentAccessToken?> ResolveAsync(string tokenHash, CancellationToken ct);
    Task MarkOpenedAsync(Guid id, CancellationToken ct);
    Task MarkCompletedAsync(Guid id, CancellationToken ct);
}

public interface IRespondentSessionRepository
{
    Task<RespondentSession> StartOrResumeAsync(RespondentAccessToken token, string? ipAddress, string? userAgent, CancellationToken ct);
    Task SaveProgressAsync(RespondentSession session, SaveRespondentProgressRequest request, CancellationToken ct);
    Task CompleteAsync(RespondentSession session, CancellationToken ct);
}

public interface IPublicResultViewRepository
{
    Task<Guid> CreateAsync(Guid organizationId, Guid diagnosticId, Guid resultId, string title, string tokenHash,
        DateTimeOffset expiresAt, bool allowReport, bool allowCertificate, CancellationToken ct);
    Task<PublicResultView?> ResolveAsync(string tokenHash, CancellationToken ct);
    Task RegisterAccessAsync(PublicResultView view, string eventType, string? ipAddress, string? userAgent, string correlationId, CancellationToken ct);
    Task RegisterCertificateDownloadAsync(PublicResultView view, string correlationId, CancellationToken ct);
}

public interface IDiagnosticInvitationBatchRepository { }
public interface IExecutiveResultPortalRepository { }
