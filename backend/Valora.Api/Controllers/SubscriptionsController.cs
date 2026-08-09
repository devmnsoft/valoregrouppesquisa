using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Api.Controllers;

[Authorize(Roles = "admin_valora,empresa_admin")]
[ApiController, Route("api/v1/subscription")]
public sealed class SubscriptionsController(ISubscriptionService subscriptions, IAuditRepository audit) : ControllerBase
{
    private Guid OrganizationId => Guid.TryParse(User.FindFirstValue("organization_id"), out var id) ? id : Guid.Empty;
    private Guid? UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpGet]
    public async Task<IActionResult> Get() => OrganizationId == Guid.Empty ? Unauthorized() : Ok(await subscriptions.GetAsync(OrganizationId));

    [HttpPut]
    public async Task<IActionResult> Update(UpdateSubscriptionRequest request)
    {
        if (OrganizationId == Guid.Empty) return Unauthorized();
        await subscriptions.UpdateAsync(OrganizationId, request);
        await Log("subscription.updated", OrganizationId);
        return NoContent();
    }

    [HttpPatch("status/{status}")]
    public async Task<IActionResult> Status(string status)
    {
        if (OrganizationId == Guid.Empty) return Unauthorized();
        await subscriptions.SetStatusAsync(OrganizationId, status);
        await Log($"subscription.{status}", OrganizationId);
        return NoContent();
    }

    [HttpGet("payments")]
    public async Task<IActionResult> Payments() => OrganizationId == Guid.Empty ? Unauthorized() : Ok(await subscriptions.ListPaymentsAsync(OrganizationId));

    [HttpPost("payments")]
    public async Task<IActionResult> RegisterPayment(RegisterManualPaymentRequest request)
    {
        if (OrganizationId == Guid.Empty) return Unauthorized();
        var payment = await subscriptions.RegisterPaymentAsync(OrganizationId, UserId, request);
        await Log("payment.manual_registered", payment.Id);
        return Created($"/api/v1/subscription/payments/{payment.Id}", payment);
    }

    private Task Log(string action, Guid entityId) => audit.AddAsync(new AuditEntry(OrganizationId, UserId, action, "subscription", entityId.ToString(), "Operação financeira registrada", "{}"));
}
