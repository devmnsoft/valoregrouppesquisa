using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Valora.Application.Experience;

namespace Valora.Api.Controllers;

[AllowAnonymous, ApiController, EnableRateLimiting("public-write")]
[Route("api/v1/respondent/{token}")]
public sealed class RespondentExperienceController(StartRespondentSessionUseCase start, SaveRespondentProgressUseCase save,
    CompleteRespondentSessionUseCase complete) : ControllerBase
{
    [HttpPost("start")]
    public async Task<IActionResult> Start(string token, CancellationToken ct)
    {
        var session = await start.ExecuteAsync(token, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), ct);
        return session is null ? LinkUnavailable() : Ok(new { session.ProgressPercent, session.Status, message = session.ProgressPercent > 0 ? "Você pode continuar de onde parou." : "Sua participação está pronta para começar." });
    }

    [HttpPut("progress")]
    public async Task<IActionResult> Progress(string token, [FromBody] SaveRespondentProgressRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var session = await start.ExecuteAsync(token, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), ct);
        if (session is null) return LinkUnavailable();
        await save.ExecuteAsync(session, request, ct);
        return Ok(new { message = "Sua resposta foi salva." });
    }

    [HttpPost("complete")]
    public async Task<IActionResult> Complete(string token, CancellationToken ct)
    {
        var session = await start.ExecuteAsync(token, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), ct);
        if (session is null) return LinkUnavailable();
        await complete.ExecuteAsync(session, ct);
        return Ok(new { message = "Participação concluída. Obrigado por contribuir." });
    }

    private ObjectResult LinkUnavailable() => StatusCode(StatusCodes.Status410Gone, new ProblemDetails { Title = "Este link expirou ou não está mais disponível.", Detail = "Solicite um novo convite à organização responsável." });
}

[AllowAnonymous, ApiController]
[Route("api/v1/public/results/{token}")]
public sealed class PublicResultExperienceController(Valora.Application.Experience.RegisterPublicResultAccessUseCase access) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Open(string token, CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;
        var result = await access.ExecuteAsync(token, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), correlationId, ct);
        return result is null
            ? StatusCode(StatusCodes.Status410Gone, new ProblemDetails { Title = "Este link expirou. Solicite um novo compartilhamento." })
            : Ok(new { result.Title, result.AllowReport, result.AllowCertificate, result.ExpiresAt, message = "Este resultado foi gerado a partir das respostas disponíveis." });
    }
}
