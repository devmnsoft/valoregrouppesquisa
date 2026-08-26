using System.Security.Cryptography;
using System.Text;

namespace Valora.Application.Experience;

public static class ExperienceToken
{
    public static string Generate() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    public static bool IsWellFormed(string token) => token.Length == 64 && token.All(Uri.IsHexDigit);
}

public sealed class RespondentPortalService(IRespondentAccessTokenRepository tokens)
{
    public async Task<RespondentAccessToken?> ResolveAsync(string rawToken, CancellationToken ct = default)
    {
        if (!ExperienceToken.IsWellFormed(rawToken)) return null;
        var token = await tokens.ResolveAsync(ExperienceToken.Hash(rawToken), ct);
        if (token is null || token.Status is "revoked" or "completed" || token.ExpiresAt <= DateTimeOffset.UtcNow) return null;
        await tokens.MarkOpenedAsync(token.Id, ct);
        return token;
    }
}

public sealed class RespondentSessionService(IRespondentSessionRepository sessions)
{
    public Task<RespondentSession> StartAsync(RespondentAccessToken token, string? ip, string? userAgent, CancellationToken ct = default) => sessions.StartOrResumeAsync(token, ip, userAgent, ct);
    public Task SaveAsync(RespondentSession session, SaveRespondentProgressRequest request, CancellationToken ct = default) => sessions.SaveProgressAsync(session, request, ct);
    public Task CompleteAsync(RespondentSession session, CancellationToken ct = default) => sessions.CompleteAsync(session, ct);
}

public sealed class PublicResultPortalService(IPublicResultViewRepository views)
{
    public async Task<PublicResultView?> OpenAsync(string rawToken, string? ip, string? userAgent, string correlationId, CancellationToken ct = default)
    {
        if (!ExperienceToken.IsWellFormed(rawToken)) return null;
        var view = await views.ResolveAsync(ExperienceToken.Hash(rawToken), ct);
        if (view is null || view.Status != "active" || view.ExpiresAt <= DateTimeOffset.UtcNow) return null;
        await views.RegisterAccessAsync(view, "public_result.opened", ip, userAgent, correlationId, ct);
        return view;
    }
}

public sealed class PublicCertificateService(IPublicResultViewRepository views)
{
    public async Task<bool> RegisterDownloadAsync(PublicResultView view, string correlationId, CancellationToken ct = default)
    {
        if (!view.AllowCertificate || view.Status != "active" || view.ExpiresAt <= DateTimeOffset.UtcNow) return false;
        await views.RegisterCertificateDownloadAsync(view, correlationId, ct);
        return true;
    }
}

public sealed class DiagnosticInvitationExperienceService;
public sealed class ExecutiveResultExperienceService;

public sealed class CreateRespondentAccessTokenUseCase(IRespondentAccessTokenRepository repository)
{
    public async Task<IssuedPublicToken> ExecuteAsync(Guid organizationId, Guid diagnosticId, Guid respondentId, DateTimeOffset expiresAt, CancellationToken ct = default)
    {
        if (expiresAt <= DateTimeOffset.UtcNow) throw new ArgumentOutOfRangeException(nameof(expiresAt));
        var raw = ExperienceToken.Generate();
        var id = await repository.CreateAsync(organizationId, diagnosticId, respondentId, ExperienceToken.Hash(raw), expiresAt, ct);
        return new(id, raw, expiresAt);
    }
}

public sealed class StartRespondentSessionUseCase(RespondentPortalService portal, RespondentSessionService sessions)
{
    public async Task<RespondentSession?> ExecuteAsync(string token, string? ip, string? userAgent, CancellationToken ct = default)
    {
        var access = await portal.ResolveAsync(token, ct);
        return access is null ? null : await sessions.StartAsync(access, ip, userAgent, ct);
    }
}
public sealed class SaveRespondentProgressUseCase(RespondentSessionService service) { public Task ExecuteAsync(RespondentSession session, SaveRespondentProgressRequest request, CancellationToken ct = default) => service.SaveAsync(session, request, ct); }
public sealed class CompleteRespondentSessionUseCase(RespondentSessionService service) { public Task ExecuteAsync(RespondentSession session, CancellationToken ct = default) => service.CompleteAsync(session, ct); }
public sealed class GeneratePublicResultViewUseCase(IPublicResultViewRepository repository)
{
    public async Task<IssuedPublicToken> ExecuteAsync(Guid organizationId, Guid diagnosticId, Guid resultId, string title, DateTimeOffset expiresAt, bool report, bool certificate, CancellationToken ct = default)
    { var raw = ExperienceToken.Generate(); var id = await repository.CreateAsync(organizationId, diagnosticId, resultId, title, ExperienceToken.Hash(raw), expiresAt, report, certificate, ct); return new(id, raw, expiresAt); }
}
public sealed class RegisterPublicResultAccessUseCase(PublicResultPortalService service) { public Task<PublicResultView?> ExecuteAsync(string token, string? ip, string? userAgent, string correlationId, CancellationToken ct = default) => service.OpenAsync(token, ip, userAgent, correlationId, ct); }
public sealed class DownloadPublicCertificateUseCase(PublicCertificateService service) { public Task<bool> ExecuteAsync(PublicResultView view, string correlationId, CancellationToken ct = default) => service.RegisterDownloadAsync(view, correlationId, ct); }
public sealed class CreateInvitationBatchUseCase;
public sealed class GenerateExecutiveResultPortalUseCase;
