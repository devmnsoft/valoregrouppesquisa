using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Forms;
using Valora.Application.Common;
using Valora.Application.Access;

namespace Valora.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/forms")]
public sealed class FormsController(
    IFormAdministrationService forms,
    ICurrentOrganizationProvider organizationProvider,
    ICurrentRequestContext currentRequest,
    ILogger<FormsController> logger) : ControllerBase
{
    private Guid UserId => currentRequest.GetCurrent().RequireUserId();

    [HttpGet]
    [Authorize(Policy = ValoraPermissions.Forms.Read)]
    public async Task<ActionResult<IReadOnlyList<FormListItemResponse>>> List([FromQuery] FormListQuery query, CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization();
        return organization.IsResolved
            ? Ok(await forms.ListAsync(organization.RequireOrganizationId(), query, cancellationToken))
            : OrganizationRequired();
    }

    [HttpGet("{formId:guid}")]
    [Authorize(Policy = ValoraPermissions.Forms.Read)]
    public async Task<ActionResult<FormDetailResponse>> Get(Guid formId, CancellationToken cancellationToken)
    {
        if (formId == Guid.Empty)
            return BadRequest(FormIdentifierRequired());

        var organization = ResolveOrganization();
        if (!organization.IsResolved) return OrganizationRequired();

        var form = await forms.GetAsync(organization.RequireOrganizationId(), formId, cancellationToken);
        return form is null
            ? NotFound(new ProblemDetails
            {
                Title = "Formulário não encontrado",
                Detail = "O formulário solicitado não existe ou não pertence à organização selecionada.",
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            })
            : Ok(form);
    }

    [HttpPost]
    [Authorize(Policy = ValoraPermissions.Forms.Create)]
    public async Task<ActionResult<FormDetailResponse>> Create(CreateFormRequest request, CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization();
        if (!organization.IsResolved) return OrganizationRequired();
        var form = await forms.CreateAsync(organization.RequireOrganizationId(), UserId, request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { formId = form.Id }, form);
    }

    [HttpPut("{formId:guid}")]
    [Authorize(Policy = ValoraPermissions.Forms.Update)]
    public async Task<ActionResult<FormDetailResponse>> Update(Guid formId, UpdateFormRequest request, CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization();
        if (!organization.IsResolved) return OrganizationRequired();
        var form = await forms.UpdateAsync(organization.RequireOrganizationId(), formId, request, cancellationToken);
        return form is null ? Conflict(ConflictDetails()) : Ok(form);
    }

    [HttpPost("{formId:guid}/publish")]
    [Authorize(Policy = ValoraPermissions.Forms.Publish)]
    public async Task<ActionResult<FormVersionResponse>> Publish(Guid formId, PublishFormVersionRequest request, CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization();
        if (!organization.IsResolved) return OrganizationRequired();
        var version = await forms.PublishAsync(organization.RequireOrganizationId(), formId, UserId, request, cancellationToken);
        return version is null ? UnprocessableEntity(new ProblemDetails { Title = "O formulário ainda não pode ser publicado", Detail = "Revise seções, perguntas e a versão antes de tentar novamente.", Status = 422 }) : Ok(version);
    }

    [HttpPost("{formId:guid}/reorder")]
    [Authorize(Policy = ValoraPermissions.Forms.Manage)]
    public async Task<ActionResult<ReorderFormItemResponse>> Reorder(Guid formId, ReorderFormItemRequest request, CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization();
        if (!organization.IsResolved) return OrganizationRequired();
        var result = await forms.ReorderAsync(organization.RequireOrganizationId(), formId, request, cancellationToken);
        return result is null ? Conflict(ConflictDetails()) : Ok(result);
    }

    [HttpDelete("{formId:guid}")]
    [Authorize(Policy = ValoraPermissions.Forms.Archive)]
    public async Task<IActionResult> Archive(Guid formId, [FromBody] ArchiveFormRequest request, CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization();
        if (!organization.IsResolved) return OrganizationRequired();
        return await forms.ArchiveAsync(organization.RequireOrganizationId(), formId, request, cancellationToken) ? NoContent() : Conflict(ConflictDetails());
    }

    [HttpGet("{formId:guid}/preview")]
    [Authorize(Policy = ValoraPermissions.Forms.Read)]
    public async Task<IActionResult> Preview(Guid formId, CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization();
        if (!organization.IsResolved) return OrganizationRequired();
        var form = await forms.PreviewAsync(organization.RequireOrganizationId(), formId, cancellationToken);
        return form is null ? NotFound() : Ok(form);
    }

    [HttpPost("{formId:guid}/duplicate")]
    [Authorize(Policy = ValoraPermissions.Forms.Create)]
    public async Task<IActionResult> Duplicate(Guid formId, DuplicateFormRequest request, CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization();
        if (!organization.IsResolved) return OrganizationRequired();
        var copy = await forms.DuplicateAsync(organization.RequireOrganizationId(), formId, UserId, request, cancellationToken);
        return copy is null ? Conflict(ConflictDetails()) : CreatedAtAction(nameof(Get), new { formId = copy.Id }, copy);
    }

    [HttpPost("{formId:guid}/versions")]
    [Authorize(Policy = ValoraPermissions.Forms.Update)]
    public async Task<IActionResult> CreateDraftVersion(Guid formId, CreateFormVersionRequest request,
        CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization();
        if (!organization.IsResolved) return OrganizationRequired();
        var form = await forms.CreateDraftVersionAsync(organization.RequireOrganizationId(), formId, UserId, request, cancellationToken);
        return form is null ? Conflict(ConflictDetails()) : Ok(form);
    }

    [HttpPost("{formId:guid}/sections")]
    [Authorize(Policy = ValoraPermissions.Forms.Manage)]
    public async Task<IActionResult> CreateSection(Guid formId, CreateFormSectionRequest request, CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization(); if (!organization.IsResolved) return OrganizationRequired();
        var item = await forms.CreateSectionAsync(organization.RequireOrganizationId(), formId, UserId, request, cancellationToken);
        return item is null ? Conflict(ConflictDetails()) : Ok(item);
    }

    [HttpPut("{formId:guid}/sections/{sectionId:guid}")]
    [Authorize(Policy = ValoraPermissions.Forms.Manage)]
    public async Task<IActionResult> UpdateSection(Guid formId, Guid sectionId, UpdateFormSectionRequest request, CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization(); if (!organization.IsResolved) return OrganizationRequired();
        var item = await forms.UpdateSectionAsync(organization.RequireOrganizationId(), formId, sectionId, UserId, request, cancellationToken);
        return item is null ? Conflict(ConflictDetails()) : Ok(item);
    }

    [HttpDelete("{formId:guid}/sections/{sectionId:guid}")]
    [Authorize(Policy = ValoraPermissions.Forms.Manage)]
    public async Task<IActionResult> DeleteSection(Guid formId, Guid sectionId, DeleteFormSectionRequest request, CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization(); if (!organization.IsResolved) return OrganizationRequired();
        return await forms.DeleteSectionAsync(organization.RequireOrganizationId(), formId, sectionId, UserId, request, cancellationToken)
            ? NoContent() : Conflict(ConflictDetails());
    }

    [HttpPost("{formId:guid}/questions")]
    [Authorize(Policy = ValoraPermissions.Forms.Manage)]
    public async Task<IActionResult> CreateQuestion(Guid formId, CreateQuestionRequest request, CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization(); if (!organization.IsResolved) return OrganizationRequired();
        var item = await forms.CreateQuestionAsync(organization.RequireOrganizationId(), formId, UserId, request, cancellationToken);
        return item is null ? Conflict(ConflictDetails()) : Ok(item);
    }

    [HttpPut("{formId:guid}/questions/{questionId:guid}")]
    [Authorize(Policy = ValoraPermissions.Forms.Manage)]
    public async Task<IActionResult> UpdateQuestion(Guid formId, Guid questionId, UpdateQuestionRequest request, CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization(); if (!organization.IsResolved) return OrganizationRequired();
        var item = await forms.UpdateQuestionAsync(organization.RequireOrganizationId(), formId, questionId, UserId, request, cancellationToken);
        return item is null ? Conflict(ConflictDetails()) : Ok(item);
    }

    [HttpDelete("{formId:guid}/questions/{questionId:guid}")]
    [Authorize(Policy = ValoraPermissions.Forms.Manage)]
    public async Task<IActionResult> DeleteQuestion(Guid formId, Guid questionId, DeleteQuestionRequest request, CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization(); if (!organization.IsResolved) return OrganizationRequired();
        return await forms.DeleteQuestionAsync(organization.RequireOrganizationId(), formId, questionId, UserId, request, cancellationToken)
            ? NoContent() : Conflict(ConflictDetails());
    }

    [HttpPost("{formId:guid}/questions/{questionId:guid}/options")]
    [Authorize(Policy = ValoraPermissions.Forms.Manage)]
    public async Task<IActionResult> CreateOption(Guid formId, Guid questionId, CreateQuestionOptionRequest request, CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization(); if (!organization.IsResolved) return OrganizationRequired();
        var item = await forms.CreateOptionAsync(organization.RequireOrganizationId(), formId, questionId, UserId, request, cancellationToken);
        return item is null ? Conflict(ConflictDetails()) : Ok(item);
    }

    [HttpPut("{formId:guid}/options/{optionId:guid}")]
    [Authorize(Policy = ValoraPermissions.Forms.Manage)]
    public async Task<IActionResult> UpdateOption(Guid formId, Guid optionId, UpdateQuestionOptionRequest request, CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization(); if (!organization.IsResolved) return OrganizationRequired();
        var item = await forms.UpdateOptionAsync(organization.RequireOrganizationId(), formId, optionId, UserId, request, cancellationToken);
        return item is null ? Conflict(ConflictDetails()) : Ok(item);
    }

    [HttpDelete("{formId:guid}/options/{optionId:guid}")]
    [Authorize(Policy = ValoraPermissions.Forms.Manage)]
    public async Task<IActionResult> DeleteOption(Guid formId, Guid optionId, DeleteQuestionOptionRequest request, CancellationToken cancellationToken)
    {
        var organization = ResolveOrganization(); if (!organization.IsResolved) return OrganizationRequired();
        return await forms.DeleteOptionAsync(organization.RequireOrganizationId(), formId, optionId, UserId, request, cancellationToken)
            ? NoContent() : Conflict(ConflictDetails());
    }

    private CurrentOrganizationContext ResolveOrganization()
    {
        var organization = organizationProvider.GetCurrent();
        if (organization.IsResolved && User.IsInRole("SuperAdmin"))
            logger.LogInformation("Super Admin acessando formulários com organização selecionada. OrganizationId={OrganizationId} Source={Source}", organization.RequireOrganizationId(), organization.Source);
        return organization;
    }

    private ObjectResult OrganizationRequired() => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
    {
        Title = "Organização não selecionada",
        Detail = "Não foi possível carregar este formulário. Verifique se a organização está selecionada e tente novamente.",
        Status = StatusCodes.Status403Forbidden,
        Extensions = { ["code"] = "ORGANIZATION_SCOPE_REQUIRED", ["correlationId"] = HttpContext.TraceIdentifier }
    });

    private ProblemDetails FormIdentifierRequired() => new()
    {
        Title = "Formulário inválido",
        Detail = "Informe um identificador de formulário válido.",
        Status = StatusCodes.Status400BadRequest,
        Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
    };

    private static ProblemDetails ConflictDetails() => new()
    {
        Title = "Este formulário foi atualizado",
        Detail = "Outra pessoa salvou uma versão mais recente enquanto você editava. Recarregue para comparar as alterações.",
        Status = StatusCodes.Status409Conflict
    };
}
