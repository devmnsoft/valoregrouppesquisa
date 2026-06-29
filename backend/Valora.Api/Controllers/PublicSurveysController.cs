using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Valora.Api.Configuration;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Security;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class PublicSurveysController(IPublicSurveyService service, IMemoryCache cache, IOptions<FreeSurveySecurityOptions> securityOptions, ILogger<PublicSurveysController> logger) : ControllerBase
{
    [HttpPost("/public/surveys/{surveyId:guid}/validate")]
    public async Task<IActionResult> Validate(Guid surveyId, ValidateSurveyRequest request)
    {
        var result = await service.ValidateAsync(surveyId, request);
        return Ok(result);
    }

    [HttpPost("/public/surveys/{surveyId:guid}/responses")]
    public async Task<IActionResult> Submit(Guid surveyId, SubmitSurveyResponseRequest request)
    {
        var blocked = CheckAbuse(surveyId, request);
        if (blocked is not null) return StatusCode(StatusCodes.Status429TooManyRequests, blocked);
        var result = await service.SubmitAsync(surveyId, request);
        return Ok(result);
    }

    private object? CheckAbuse(Guid surveyId, SubmitSurveyResponseRequest request)
    {
        var opt = securityOptions.Value;
        if (!opt.Enabled) return null;
        var ipHash = Hash(HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        var email = request.Participant != null && request.Participant.TryGetValue("email", out var e) ? e?.ToString()?.Trim().ToLowerInvariant() : string.Empty;
        var emailHash = Hash(email ?? string.Empty);
        var tokenHash = Hash(request.Token ?? string.Empty);
        var userAgent = Request.Headers.UserAgent.ToString();
        string? reason = null;
        if (string.IsNullOrWhiteSpace(userAgent) || userAgent.Length < 6 || userAgent.Contains("bot", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("curl", StringComparison.OrdinalIgnoreCase)) reason = "suspicious-user-agent";
        if (opt.EnableHoneypot && request.Participant != null && request.Participant.TryGetValue("website", out var hp) && !string.IsNullOrWhiteSpace(hp?.ToString())) reason = "honeypot-filled";
        if (request.Participant != null && request.Participant.TryGetValue("formStartedAt", out var started) && DateTimeOffset.TryParse(started?.ToString(), out var start) && DateTimeOffset.UtcNow.Subtract(start).TotalSeconds < opt.MinSecondsToSubmit) reason = "minimum-fill-time";
        reason ??= Increment($"free-survey:ip:{ipHash}", opt.MaxSubmissionsPerIpPerHour, TimeSpan.FromHours(1)) ? "ip-rate-limit" : null;
        reason ??= !string.IsNullOrWhiteSpace(email) && Increment($"free-survey:email:{emailHash}", opt.MaxSubmissionsPerEmailPerDay, TimeSpan.FromDays(1)) ? "email-rate-limit" : null;
        reason ??= Increment($"free-survey:token:{tokenHash}", opt.MaxSubmissionsPerTokenPerHour, TimeSpan.FromHours(1)) ? "token-rate-limit" : null;
        var duplicateKey = $"free-survey:duplicate:{surveyId}:{emailHash}:{tokenHash}";
        if (cache.TryGetValue(duplicateKey, out _)) reason ??= "duplicate-submission-window";
        cache.Set(duplicateKey, true, TimeSpan.FromSeconds(Math.Max(5, opt.DuplicateSubmissionWindowSeconds)));
        if (reason is null) return null;
        logger.LogWarning("Free survey submission blocked. SurveyId={SurveyId} Reason={Reason} IpHash={IpHash} EmailHash={EmailHash} UserAgentHash={UserAgentHash}", surveyId, reason, ipHash, emailHash, Hash(userAgent));
        return new { ok = false, code = "FREE_SURVEY_SECURITY_BLOCKED", reason, correlationId = HttpContext.TraceIdentifier };
    }

    private bool Increment(string key, int max, TimeSpan ttl)
    {
        var count = cache.Get<int?>(key) ?? 0;
        count++;
        cache.Set(key, count, ttl);
        return count > max;
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
