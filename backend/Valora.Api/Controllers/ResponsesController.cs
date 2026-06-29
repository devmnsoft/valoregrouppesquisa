using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class ResponsesController(IResponseRepository responses) : ControllerBase
{
    [HttpGet("/responses/{responseId:guid}/result")]
    public async Task<IActionResult> Result(Guid responseId)
    {
        var result = await responses.GetResultAsync(responseId);
        return result is null ? NotFound(new { ok = false }) : Ok(result);
    }
}
