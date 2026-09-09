using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Experience;

namespace Valora.Api.Controllers;

[AllowAnonymous, ApiController]
[Route("api/v1/public/results/{token}")]
public sealed class PublicResultExperienceController(RegisterPublicResultAccessUseCase access) : ControllerBase {
    [HttpGet]
    public async Task<IActionResult> Open(string token, CancellationToken cancellationToken) {
        var result = await access.ExecuteAsync(token, HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(), HttpContext.TraceIdentifier, cancellationToken);
        return result is null
            ? StatusCode(StatusCodes.Status410Gone, new ProblemDetails { Title = "Este link expirou. Solicite um novo compartilhamento." })
            : Ok(new { result.Title, result.AllowReport, result.AllowCertificate, result.ExpiresAt, message = "Este resultado foi gerado a partir das respostas disponíveis." });
    }
}
