using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class PublicResultsController(IResponseRepository responses) : ControllerBase
{
    [HttpPost("/public/results/{responseId}")]
    public async Task<IActionResult> Result(string responseId, PublicResultRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ResultToken)) return BadRequest(new { ok = false, message = "Token do resultado obrigatório.", code = "RESULT_TOKEN_REQUIRED" });
        if (!Guid.TryParse(responseId, out var parsedResponseId)) return BadRequest(new { ok = false, message = "Identificador do resultado inválido.", code = "INVALID_RESPONSE_ID" });
        var result = await responses.GetResultAsync(parsedResponseId);
        return result is null
            ? NotFound(new { ok = false, message = "Resultado não encontrado.", code = "RESULT_NOT_FOUND" })
            : Ok(new { ok = true, result, survey = new { }, company = new { }, certificate = new { responseId = parsedResponseId, status = "metadata-ready" } });
    }
}
