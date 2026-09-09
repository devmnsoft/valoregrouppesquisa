using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common;
using Valora.Application.Indicators;

namespace Valora.Api.Controllers;

[Authorize, ApiController, Route("api/v1/analytics/snapshots")]
public sealed class AnalyticsSnapshotsController(AnalyticsSnapshotService service, ICurrentRequestContext currentRequest) : ControllerBase
{
    private Guid OrganizationId => currentRequest.GetCurrent().RequireOrganizationId();
    private Guid UserId => currentRequest.GetCurrent().RequireUserId();

    [HttpGet]
    public async Task<ActionResult> List(CancellationToken cancellationToken) =>
        Ok(await service.List(OrganizationId, cancellationToken));

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateSnapshotRequest request, CancellationToken cancellationToken) =>
        Ok(new { id = await service.Create(OrganizationId, UserId, request.Name, cancellationToken), eventName = "analytics.snapshot.created" });
}

public sealed record CreateSnapshotRequest([property: Required, StringLength(160)] string Name);
