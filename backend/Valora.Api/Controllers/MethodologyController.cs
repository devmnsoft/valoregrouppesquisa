using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Methodology;

namespace Valora.Api.Controllers;

[Authorize, ApiController, Route("api/v1/methodology")]
public sealed class MethodologyController(IMethodologyService methodology, ValoraInferenceEngine inference,
    MethodologyVersionService versions, CreateMethodologyVersionUseCase createVersion,
    CloneMethodologyVersionUseCase cloneVersion, ValidateMethodologyConsistencyUseCase validate,
    PublishMethodologyVersionUseCase publish) : ControllerBase
{
    [HttpGet("studio")]
    public async Task<IActionResult> Studio(CancellationToken ct)
    {
        var items = await versions.ListAsync(ct);
        var active = items.FirstOrDefault(x => x.IsOfficial && x.Status == "published");
        IReadOnlyList<MethodologyValidationIssue> issues = active is null ? [] : await validate.ExecuteAsync(active.Id, ct);
        return Ok(new MethodologyStudioDashboard(active, issues.Count(x => x.Severity == "critical"), issues, items));
    }

    [HttpGet("versions")]
    public async Task<IActionResult> Versions(CancellationToken ct) => Ok(await versions.ListAsync(ct));

    [HttpPost("versions")]
    public async Task<IActionResult> Create([FromBody] CreateVersionRequest request, CancellationToken ct) =>
        Ok(new { id = await createVersion.ExecuteAsync(request.Code, request.Name, request.Description, null, ct) });

    [HttpPost("versions/{id:guid}/clone")]
    public async Task<IActionResult> Clone(Guid id, [FromBody] CloneVersionRequest request, CancellationToken ct) =>
        Ok(new { id = await cloneVersion.ExecuteAsync(id, request.Code, request.Name, null, ct) });

    [HttpGet("versions/{id:guid}/validation")]
    public async Task<IActionResult> Validation(Guid id, CancellationToken ct) => Ok(await validate.ExecuteAsync(id, ct));

    [HttpPost("versions/{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, [FromBody] PublishVersionRequest request, CancellationToken ct)
    { await publish.ExecuteAsync(id, null, request.Justification, ct); return Ok(new { status = "published" }); }
    [HttpGet("concepts")]
    public async Task<IActionResult> Concepts([FromQuery] string? search, [FromQuery] string? pillar, CancellationToken ct) => Ok(await methodology.ListConceptsAsync(search, pillar, ct));

    [HttpGet("concepts/{code}")]
    public async Task<IActionResult> Concept(string code, CancellationToken ct)
    {
        var concept = await methodology.GetConceptAsync(code, ct);
        if (concept is null) return NotFound(new { code = "METHODOLOGY_CONCEPT_NOT_FOUND", message = "Conceito metodológico não encontrado." });
        return Ok(new { concept, relations = await methodology.ListRelationsAsync(code, ct), evidence = await methodology.ListEvidenceAsync(code, ct) });
    }

    [HttpGet("cognitive-map")]
    public async Task<IActionResult> CognitiveMap([FromQuery] string? concept, CancellationToken ct) => Ok(await methodology.ListRelationsAsync(concept, ct));

    [HttpPost("inferences/evaluate")]
    public IActionResult Evaluate([FromBody] InferenceRequest request) => Ok(inference.Infer(request));
}

public sealed record CreateVersionRequest(string Code, string Name, string? Description);
public sealed record CloneVersionRequest(string Code, string Name);
public sealed record PublishVersionRequest(string Justification);
