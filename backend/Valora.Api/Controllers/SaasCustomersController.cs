using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Access;
using Valora.Application.SaasAdministration;

namespace Valora.Api.Controllers;

[Authorize(Roles = ValoraAccessCatalog.PlatformRole)]
[ApiController]
[Route("api/v1/saas/customers")]
public sealed class SaasCustomersController(SaasCustomerService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = ValoraPermissions.SaasCustomers.View)]
    public async Task<IActionResult> List(CancellationToken cancellationToken) => Ok(await service.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = ValoraPermissions.SaasCustomers.View)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var customer = await service.GetAsync(id, cancellationToken);
        return customer is null ? NotFound(new { message = "Cliente não encontrado." }) : Ok(customer);
    }

    [HttpPost]
    [Authorize(Policy = ValoraPermissions.SaasCustomers.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateSaasCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = customer.Id }, customer);
    }

    [HttpPost("{id:guid}/block")]
    [Authorize(Policy = ValoraPermissions.SaasCustomers.Block)]
    public Task<IActionResult> Block(Guid id, [FromBody] AccessChangeRequest request, CancellationToken cancellationToken) => ChangeAccess(id, true, request, cancellationToken);

    [HttpPost("{id:guid}/unblock")]
    [Authorize(Policy = ValoraPermissions.SaasCustomers.Block)]
    public Task<IActionResult> Unblock(Guid id, [FromBody] AccessChangeRequest request, CancellationToken cancellationToken) => ChangeAccess(id, false, request, cancellationToken);

    private async Task<IActionResult> ChangeAccess(Guid id, bool blocked, AccessChangeRequest request, CancellationToken cancellationToken)
    {
        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : Guid.Empty;
        var correlationId = HttpContext.TraceIdentifier;
        return await service.SetBlockedAsync(id, blocked, actorId, request.Reason, correlationId, cancellationToken)
            ? NoContent()
            : NotFound(new { message = "Cliente não encontrado." });
    }
}

public sealed record AccessChangeRequest([property: Required, StringLength(500, MinimumLength = 3)] string Reason);
