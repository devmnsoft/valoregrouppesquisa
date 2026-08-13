using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Methodology;

namespace Valora.Api.Controllers;

[Authorize, ApiController, Route("api/v1/methodology")]
public sealed class MethodologyController(IMethodologyService methodology, ValoraInferenceEngine inference) : ControllerBase
{
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
