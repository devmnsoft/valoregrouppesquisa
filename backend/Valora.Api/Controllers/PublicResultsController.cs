using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class PublicResultsController(IPublicResultService service) : ControllerBase
{
    [HttpPost("/public/results/{responseId:guid}")]
    public async Task<IActionResult> Result(Guid responseId, PublicResultRequest request)
    {
        var result = await service.GetAsync(responseId, request);
        return Ok(result);
    }
}
