using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Forms;

namespace Valora.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/forms")]
public sealed class FormsController(IFormAdministrationService forms) : ControllerBase
{
    private Guid OrganizationId => Guid.TryParse(User.FindFirstValue("organization_id"), out var id) ? id : Guid.Empty;
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FormListItemResponse>>> List([FromQuery] FormListQuery query, CancellationToken cancellationToken) =>
        Ok(await forms.ListAsync(OrganizationId, query, cancellationToken));

    [HttpGet("{formId:guid}")]
    public async Task<ActionResult<FormDetailResponse>> Get(Guid formId, CancellationToken cancellationToken)
    {
        var form = await forms.GetAsync(OrganizationId, formId, cancellationToken);
        return form is null ? NotFound() : Ok(form);
    }

    [HttpPost]
    public async Task<ActionResult<FormDetailResponse>> Create(CreateFormRequest request, CancellationToken cancellationToken)
    {
        var form = await forms.CreateAsync(OrganizationId, UserId, request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { formId = form.Id }, form);
    }

    [HttpPut("{formId:guid}")]
    public async Task<ActionResult<FormDetailResponse>> Update(Guid formId, UpdateFormRequest request, CancellationToken cancellationToken)
    {
        var form = await forms.UpdateAsync(OrganizationId, formId, request, cancellationToken);
        return form is null ? Conflict(ConflictDetails()) : Ok(form);
    }

    [HttpPost("{formId:guid}/publish")]
    public async Task<ActionResult<FormVersionResponse>> Publish(Guid formId, PublishFormVersionRequest request, CancellationToken cancellationToken)
    {
        var version = await forms.PublishAsync(OrganizationId, formId, UserId, request, cancellationToken);
        return version is null ? UnprocessableEntity(new ProblemDetails { Title = "O formulário ainda não pode ser publicado", Detail = "Revise seções, perguntas e a versão antes de tentar novamente.", Status = 422 }) : Ok(version);
    }

    [HttpPost("{formId:guid}/reorder")]
    public async Task<ActionResult<ReorderFormItemResponse>> Reorder(Guid formId, ReorderFormItemRequest request, CancellationToken cancellationToken)
    {
        var result = await forms.ReorderAsync(OrganizationId, formId, request, cancellationToken);
        return result is null ? Conflict(ConflictDetails()) : Ok(result);
    }

    [HttpDelete("{formId:guid}")]
    public async Task<IActionResult> Archive(Guid formId, [FromBody] ArchiveFormRequest request, CancellationToken cancellationToken) =>
        await forms.ArchiveAsync(OrganizationId, formId, request, cancellationToken) ? NoContent() : Conflict(ConflictDetails());

    private static ProblemDetails ConflictDetails() => new()
    {
        Title = "Este formulário foi atualizado",
        Detail = "Outra pessoa salvou uma versão mais recente enquanto você editava. Recarregue para comparar as alterações.",
        Status = StatusCodes.Status409Conflict
    };
}
