using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Experience;
using Valora.Application.Forms;

namespace Valora.Api.Controllers;

[Authorize(Roles = "admin_valora,consultor_valora,empresa_admin,gestor_pesquisa")]
[ApiController]
[Route("api/v1/experience")]
public sealed class ExperienceController(IFormAdministrationService forms, IAuditRepository audit) : ControllerBase
{
    private Guid OrganizationId => Guid.TryParse(User.FindFirstValue("organization_id"), out var id) ? id : Guid.Empty;
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    [HttpGet("templates")]
    public IActionResult Templates() => Ok(new { items = OfficialTemplateCatalog.All });

    [HttpPost("templates/{code}/use")]
    public async Task<IActionResult> UseTemplate(string code, CancellationToken cancellationToken)
    {
        var template = OfficialTemplateCatalog.Find(code);
        if (template is null) return NotFound(new ProblemDetails { Title = "Template não encontrado." });
        if (OrganizationId == Guid.Empty || UserId == Guid.Empty) return Unauthorized();
        var form = await forms.CreateAsync(OrganizationId, UserId,
            new CreateFormRequest(template.Name, template.Description, template.Code, template.EstimatedMinutes), cancellationToken);
        await audit.AddAsync(new AuditEntry(OrganizationId, UserId, "template.used", "form", form.Id.ToString(), $"Template {template.Name} utilizado", "{}"));
        return Created($"/api/v1/forms/{form.Id}", new { formId = form.Id, builderUrl = $"/Forms/{form.Id}/Builder" });
    }
}
