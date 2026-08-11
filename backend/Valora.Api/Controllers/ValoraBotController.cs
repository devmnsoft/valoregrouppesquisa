using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.ValoraBot;

namespace Valora.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/valorabot")]
public sealed class ValoraBotController(IValoraBotService service) : ControllerBase
{
    [HttpPost("ask")]
    public async Task<ActionResult<ValoraBotAnswerDto>> Ask([FromBody] ValoraBotAskRequest request, CancellationToken ct) => Ok(await service.AskAsync(request, ct));

    [HttpPost("feedback")]
    public async Task<IActionResult> Feedback([FromBody] ValoraBotFeedbackRequest request, CancellationToken ct)
    {
        await service.RegisterFeedbackAsync(request, ct);
        return NoContent();
    }
}
