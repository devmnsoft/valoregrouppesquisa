using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Access;
using Valora.Application.Common;
using Valora.Application.Contracts;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class ResponsesController(IResponseRepository responses, ICurrentRequestContext currentRequest) : ControllerBase {
    [HttpGet("/responses/{responseId:guid}/result")]
    [Authorize(Policy = ValoraPermissions.Results.Read)]
    public async Task<IActionResult> Result(Guid responseId) {
        if (!Guid.TryParse(User.FindFirstValue("organization_id") ?? User.FindFirstValue("organizationId"), out var organizationId))
            return Unauthorized(new { ok = false, code = "ORGANIZATION_SCOPE_REQUIRED", correlationId = HttpContext.TraceIdentifier });

        // This legacy administrative route must never use the unscoped public-result lookup.
        // Public consumers use /public/results/{responseId} with the opaque result token.
        var context = currentRequest.GetCurrent();
        var canReadPlans = context.IsGlobalAdministrator || context.Permissions.Contains(ValoraPermissions.Action.Read, StringComparer.OrdinalIgnoreCase);
        var organizationWide = context.IsGlobalAdministrator || context.Roles.Contains("admin_cliente", StringComparer.OrdinalIgnoreCase);
        var result = await responses.GetAdminAsync(organizationId, responseId, context.UserId, canReadPlans, organizationWide);
        return result is null ? NotFound(new { ok = false }) : Ok(result);
    }
}
