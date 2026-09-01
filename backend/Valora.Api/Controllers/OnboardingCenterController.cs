using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Onboarding;

namespace Valora.Api.Controllers;

[Authorize, ApiController]
public sealed class OnboardingCenterController(
    OnboardingFlowService flow, OnboardingProgressService progress, OnboardingChecklistService checklist,
    CustomerAdoptionService adoption, CustomerHealthScoreService health, CustomerSuccessService success) : ControllerBase
{
    [HttpGet("/api/v1/onboarding")]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await flow.GetAsync(OrganizationId(), ct));
    [HttpGet("/api/v1/onboarding/progress")]
    public async Task<IActionResult> GetProgress(CancellationToken ct) => Ok(await progress.GetAsync(OrganizationId(), ct));
    [HttpPost("/api/v1/onboarding/steps/{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CompleteOnboardingStepRequest request, CancellationToken ct) { await progress.CompleteAsync(OrganizationId(), UserId(), id, request.Evidence, ct); return NoContent(); }
    [HttpPost("/api/v1/onboarding/steps/{id:guid}/skip")]
    public async Task<IActionResult> Skip(Guid id, CancellationToken ct) { await progress.SkipAsync(OrganizationId(), UserId(), id, ct); return NoContent(); }
    [HttpGet("/api/v1/onboarding/checklist")]
    public async Task<IActionResult> Checklist(CancellationToken ct) => Ok(await checklist.GetAsync(OrganizationId(), ct));
    [HttpPost("/api/v1/onboarding/checklist/items/{id:guid}/complete")]
    public async Task<IActionResult> CompleteItem(Guid id, CancellationToken ct) { await checklist.CompleteAsync(OrganizationId(), UserId(), id, ct); return NoContent(); }
    [HttpGet("/api/v1/customer-success/health-score")]
    public async Task<IActionResult> Health(CancellationToken ct) => Ok(await health.GetAsync(OrganizationId(), ct));
    [HttpGet("/api/v1/customer-success/adoption")]
    public async Task<IActionResult> Adoption(CancellationToken ct) => Ok(await adoption.GetAsync(OrganizationId(), ct));
    [HttpPost("/api/v1/customer-success/tasks")]
    public async Task<IActionResult> Task(CreateCustomerSuccessTaskRequest request, CancellationToken ct) => Ok(new { id = await success.CreateTaskAsync(OrganizationId(), UserId(), request, ct) });
    [HttpPost("/api/v1/customer-success/notes")]
    public async Task<IActionResult> Note(CreateCustomerSuccessNoteRequest request, CancellationToken ct) => Ok(new { id = await success.CreateNoteAsync(OrganizationId(), UserId(), request, ct) });

    private Guid OrganizationId()
    {
        var claim = User.FindFirstValue("organization_id");
        if (Guid.TryParse(claim, out var id) && id != Guid.Empty) return id;
        var isPlatformAdmin = User.IsInRole("admin_valora") || User.IsInRole("super_admin");
        if (isPlatformAdmin && Guid.TryParse(Request.Headers["X-Organization-Id"].FirstOrDefault(), out id) && id != Guid.Empty) return id;
        throw new ValidationException("Selecione uma organização válida para continuar.");
    }
    private Guid UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;
}
