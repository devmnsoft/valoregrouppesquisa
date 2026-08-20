using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Valora.Application.Integrations;

namespace Valora.Api.Controllers;

[AllowAnonymous, ApiController, Route("api/public/v1")]
public sealed class PublicIntegrationsController(ApiKeyAuthenticator authenticator, IIntegrationRepository repository, IMemoryCache cache) : ControllerBase
{
    [HttpGet("organizations/{id:guid}/summary")]
    public Task<IActionResult> Organization(Guid id, CancellationToken ct) => Read("organizations", id, IntegrationScopes.OrganizationsRead, ct);
    [HttpGet("diagnostics/{id:guid}/summary")]
    public Task<IActionResult> Diagnostic(Guid id, CancellationToken ct) => Read("diagnostics", id, IntegrationScopes.DiagnosticsRead, ct);
    [HttpGet("diagnostics/{id:guid}/scores")]
    public Task<IActionResult> Scores(Guid id, CancellationToken ct) => Read("diagnostics", id, IntegrationScopes.DiagnosticsRead, ct);
    [HttpGet("diagnostics/{id:guid}/dimensions")]
    public Task<IActionResult> Dimensions(Guid id, CancellationToken ct) => Read("diagnostics", id, IntegrationScopes.DiagnosticsRead, ct);
    [HttpGet("reports/{id:guid}/metadata")]
    public Task<IActionResult> Report(Guid id, CancellationToken ct) => Read("reports", id, IntegrationScopes.ReportsRead, ct);
    [HttpGet("benchmark/{id:guid}")]
    public Task<IActionResult> Benchmark(Guid id, CancellationToken ct) => Read("benchmark", id, IntegrationScopes.BenchmarkRead, ct);
    [HttpGet("evolution/{organizationId:guid}")]
    public Task<IActionResult> Evolution(Guid organizationId, CancellationToken ct) => Read("evolution", organizationId, IntegrationScopes.EvolutionRead, ct);

    [HttpGet("certificates/{code}/validation")]
    public async Task<IActionResult> Certificate(string code, CancellationToken ct)
    {
        var auth = await Authenticate(IntegrationScopes.CertificatesValidate, ct);
        if (auth.Result is not null) return auth.Result;
        var data = await repository.CertificateAsync(code, ct);
        return await Finish(auth.Key!, data, IntegrationScopes.CertificatesValidate, ct);
    }

    private async Task<IActionResult> Read(string resource, Guid id, string scope, CancellationToken ct)
    {
        var auth = await Authenticate(scope, ct);
        if (auth.Result is not null) return auth.Result;
        return await Finish(auth.Key!, await repository.PublicDataAsync(resource, id, ct), scope, ct);
    }

    private async Task<(AuthenticatedApiKey? Key, IActionResult? Result)> Authenticate(string scope, CancellationToken ct)
    {
        var presented = Request.Headers["X-API-Key"].FirstOrDefault() ?? Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
        var prefix = presented is { Length: >= 12 } ? presented[..12] : "missing";
        var key = await authenticator.AuthenticateAsync(presented, ct);
        if (key is null) { await Log(null, prefix, 401, scope, ct); return (null, Unauthorized(new { code = "INVALID_API_KEY", message = "Informe uma API Key válida." })); }
        var bucket = $"public-api:{key.Id}:{DateTime.UtcNow:yyyyMMddHHmm}";
        var count = cache.GetOrCreate(bucket, e => { e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2); return 0; });
        cache.Set(bucket, count + 1, TimeSpan.FromMinutes(2));
        if (count >= 119) { Response.Headers["Retry-After"] = "60"; await Log(key, prefix, 429, scope, ct); return (key, StatusCode(429, new { code = "RATE_LIMITED", message = "Limite temporário atingido. Tente novamente em instantes." })); }
        if (!ApiKeyAuthenticator.HasScope(key, scope)) { await Log(key, prefix, 403, scope, ct); return (key, StatusCode(403, new { code = "INSUFFICIENT_SCOPE", message = "Esta chave não possui permissão para este recurso." })); }
        return (key, null);
    }

    private async Task<IActionResult> Finish(AuthenticatedApiKey key, PublicDataResult? result, string scope, CancellationToken ct)
    {
        if (result is null) { await Log(key, "authorized", 404, scope, ct); return NotFound(); }
        if (result.OrganizationId != key.OrganizationId) { await Log(key, "authorized", 404, scope, ct); return NotFound(); }
        await Log(key, "authorized", 200, scope, ct);
        return Ok(result.Data);
    }
    private Task Log(AuthenticatedApiKey? key, string prefix, int status, string scope, CancellationToken ct) => repository.RecordApiUseAsync(key, prefix, Request.Path, status, scope, HttpContext.TraceIdentifier, ct);
}
