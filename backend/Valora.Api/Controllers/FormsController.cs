using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Forms;
using Valora.Application.Common;

namespace Valora.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/forms")]
public sealed class FormsController(
    IFormAdministrationService forms,
    ICurrentOrganizationProvider organizationProvider,
    ILogger<FormsController> logger) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FormListItemResponse>>> List([FromQuery] FormListQuery query, CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization();
        return organization.IsResolved
            ? Ok(await forms.ListAsync(organization.OrganizationId, query, cancellationToken))
            : OrganizationRequired();
    }

    [HttpGet("{formId:guid}")]
    public async Task<ActionResult<FormDetailResponse>> Get(Guid formId, CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization();
        if (!organization.IsResolved) return OrganizationRequired();
        var form = await forms.GetAsync(organization.OrganizationId, formId, cancellationToken);
        return form is null ? NotFound() : Ok(form);
    }

    [HttpPost]
    public async Task<ActionResult<FormDetailResponse>> Create(CreateFormRequest request, CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization();
        if (!organization.IsResolved) return OrganizationRequired();
        var form = await forms.CreateAsync(organization.OrganizationId, UserId, request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { formId = form.Id }, form);
    }

    [HttpPut("{formId:guid}")]
    public async Task<ActionResult<FormDetailResponse>> Update(Guid formId, UpdateFormRequest request, CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization();
        if (!organization.IsResolved) return OrganizationRequired();
        var form = await forms.UpdateAsync(organization.OrganizationId, formId, request, cancellationToken);
        return form is null ? Conflict(ConflictDetails()) : Ok(form);
    }

    [HttpPost("{formId:guid}/publish")]
    public async Task<ActionResult<FormVersionResponse>> Publish(Guid formId, PublishFormVersionRequest request, CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization();
        if (!organization.IsResolved) return OrganizationRequired();
        var version = await forms.PublishAsync(organization.OrganizationId, formId, UserId, request, cancellationToken);
        return version is null ? UnprocessableEntity(new ProblemDetails { Title = "O formulário ainda não pode ser publicado", Detail = "Revise seções, perguntas e a versão antes de tentar novamente.", Status = 422 }) : Ok(version);
    }

    [HttpPost("{formId:guid}/reorder")]
    public async Task<ActionResult<ReorderFormItemResponse>> Reorder(Guid formId, ReorderFormItemRequest request, CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization();
        if (!organization.IsResolved) return OrganizationRequired();
        var result = await forms.ReorderAsync(organization.OrganizationId, formId, request, cancellationToken);
        return result is null ? Conflict(ConflictDetails()) : Ok(result);
    }

    [HttpDelete("{formId:guid}")]
    public async Task<IActionResult> Archive(Guid formId, [FromBody] ArchiveFormRequest request, CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization();
        if (!organization.IsResolved) return OrganizationRequired();
        return await forms.ArchiveAsync(organization.OrganizationId, formId, request, cancellationToken) ? NoContent() : Conflict(ConflictDetails());
    }

    private CurrentOrganizationContext ResolveOrganization()
    {
        var organization = organizationProvider.GetCurrent();
        if (organization.IsResolved && User.IsInRole("SuperAdmin"))
            logger.LogInformation("Super Admin acessando formulários com organização selecionada. OrganizationId={OrganizationId} Source={Source}", organization.OrganizationId, organization.Source);
        return organization;
    }

    private ObjectResult OrganizationRequired() => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
    {
        Title = "Organização não selecionada",
        Detail = CurrentOrganizationContext.RequiredMessage,
        Status = StatusCodes.Status403Forbidden,
        Extensions = { ["code"] = "ORGANIZATION_SCOPE_REQUIRED", ["correlationId"] = HttpContext.TraceIdentifier }
    });

    private static ProblemDetails ConflictDetails() => new()
    {
        Title = "Este formulário foi atualizado",
        Detail = "Outra pessoa salvou uma versão mais recente enquanto você editava. Recarregue para comparar as alterações.",
        Status = StatusCodes.Status409Conflict
    };
}
