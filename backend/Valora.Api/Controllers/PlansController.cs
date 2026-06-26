using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class PlansController(IPlanRepository plans) : ControllerBase
{
    [HttpGet("/plans/public")]
    public async Task<IActionResult> Public()
    {
        return Ok(await plans.GetPublicPlansAsync());
    }

    [HttpGet("/plans/{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var plan = await plans.GetByIdAsync(id);
        return plan is null ? NotFound(new { ok = false }) : Ok(plan);
    }
}
