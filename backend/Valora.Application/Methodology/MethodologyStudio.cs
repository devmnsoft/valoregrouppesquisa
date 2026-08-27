using System.ComponentModel.DataAnnotations;

namespace Valora.Application.Methodology;

public sealed record CreateMethodologyVersionRequest(
    [property: Required, StringLength(80)] string Code,
    [property: Required, StringLength(160)] string Name,
    [property: StringLength(2000)] string? Description);

public sealed record UpdateMethodologyDimensionRequest(
    [property: Required, StringLength(80)] string Code,
    [property: Required, StringLength(160)] string Name,
    [property: Required, StringLength(2000)] string Description,
    [property: Range(typeof(decimal), "0.0001", "100")] decimal Weight);

public sealed record CreateConceptRequest(
    [property: Required, StringLength(80)] string Code,
    [property: Required, StringLength(160)] string Name,
    [property: Required] IReadOnlyCollection<Guid> DimensionIds,
    [property: Required] string EvidenceCriteria);

public sealed record CreateMaturityLevelRequest(
    [property: Required] string Name, int MaturityLevel, decimal ScoreMin, decimal ScoreMax,
    [property: Required] string VerifiableCriteria);
public sealed record CreateEvidenceCriteriaRequest(
    [property: Required] Guid ConceptId, [property: Required] string Name,
    [property: Required] string ExpectedSource, [property: Range(1, 5)] int EvidenceStrength);
public sealed record CreateQuestionBankItemRequest(
    [property: Required] string Code, [property: Required] string QuestionText,
    [property: Required] string ResponseType, decimal Weight,
    IReadOnlyCollection<Guid> DimensionIds, IReadOnlyCollection<Guid> ConceptIds, bool IsEvaluative = true);
public sealed record CreateDiagnosticTemplateRequest(
    [property: Required] string Code, [property: Required] string Name,
    [property: Required] IReadOnlyCollection<Guid> SectionIds, Guid? ScoringRuleId);
public sealed record PublishDiagnosticTemplateRequest(
    [property: Required] Guid TemplateId, [property: Required, StringLength(1000)] string Justification);

public sealed record MethodologyVersionSummary(Guid Id, string Code, string Name, string Status, int VersionNumber,
    bool IsOfficial, DateTimeOffset? PublishedAt, int Concepts, int Indexes, int Questions, int Prompts);
public sealed record MethodologyValidationIssue(string Code, string Severity, string Entity, string Message);
public sealed record MethodologyStudioDashboard(MethodologyVersionSummary? ActiveVersion, int CriticalIssues,
    IReadOnlyList<MethodologyValidationIssue> Issues, IReadOnlyList<MethodologyVersionSummary> Versions);

public interface IMethodologyStudioRepository
{
    Task<IReadOnlyList<MethodologyVersionSummary>> ListVersionsAsync(CancellationToken ct);
    Task<Guid> CreateDraftAsync(string code, string name, string? description, Guid? sourceVersionId, Guid? actorId, CancellationToken ct);
    Task<IReadOnlyList<MethodologyValidationIssue>> ValidateAsync(Guid versionId, CancellationToken ct);
    Task PublishAsync(Guid versionId, Guid? actorId, string justification, CancellationToken ct);
}

public sealed class MethodologyValidationService(IMethodologyStudioRepository repository)
{
    public Task<IReadOnlyList<MethodologyValidationIssue>> ValidateAsync(Guid versionId, CancellationToken ct) => repository.ValidateAsync(versionId, ct);

    public static void EnsureDimension(UpdateMethodologyDimensionRequest request)
    {
        EnsureAnnotations(request);
        if (request.Weight <= 0) throw new ValidationException("O peso da dimensão deve ser maior que zero.");
    }

    public static void EnsureConcept(CreateConceptRequest request)
    {
        EnsureAnnotations(request);
        if (request.DimensionIds.Count == 0 || request.DimensionIds.Any(x => x == Guid.Empty))
            throw new ValidationException("O conceito deve estar vinculado a pelo menos uma dimensão válida.");
        if (string.IsNullOrWhiteSpace(request.EvidenceCriteria))
            throw new ValidationException("O conceito deve possuir critérios de evidência verificáveis.");
    }

    public static void EnsureQuestion(CreateQuestionBankItemRequest request)
    {
        EnsureAnnotations(request);
        var validTypes = new[] { "scale", "scale_1_5", "single_choice", "multiple_choice", "boolean", "text", "number" };
        if (!validTypes.Contains(request.ResponseType, StringComparer.OrdinalIgnoreCase))
            throw new ValidationException("Selecione um tipo de resposta válido.");
        if (request.DimensionIds.Count == 0 && request.ConceptIds.Count == 0)
            throw new ValidationException("A pergunta deve estar vinculada a uma dimensão ou conceito.");
        if (request.DimensionIds.Concat(request.ConceptIds).Any(x => x == Guid.Empty))
            throw new ValidationException("A pergunta possui um vínculo inválido.");
        if (request.IsEvaluative && request.Weight <= 0)
            throw new ValidationException("Perguntas avaliativas devem possuir peso maior que zero.");
    }

    public static void EnsureTemplate(CreateDiagnosticTemplateRequest request)
    {
        EnsureAnnotations(request);
        if (request.SectionIds.Count == 0 || request.SectionIds.Any(x => x == Guid.Empty))
            throw new ValidationException("O template deve possuir ao menos uma seção válida.");
        if (!request.ScoringRuleId.HasValue || request.ScoringRuleId.Value == Guid.Empty)
            throw new ValidationException("O template precisa de uma regra de cálculo versionada.");
    }

    private static void EnsureAnnotations(object request) =>
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
}
public sealed class MethodologyVersionService(IMethodologyStudioRepository repository)
{
    public Task<IReadOnlyList<MethodologyVersionSummary>> ListAsync(CancellationToken ct) => repository.ListVersionsAsync(ct);
}
public sealed class MethodologyPublicationService(IMethodologyStudioRepository repository, MethodologyValidationService validation)
{
    public async Task PublishAsync(Guid id, Guid? actor, string justification, CancellationToken ct)
    {
        var issues = await validation.ValidateAsync(id, ct);
        if (issues.Any(x => x.Severity == "critical")) throw new InvalidOperationException("A versão possui inconsistências críticas e não pode ser publicada.");
        await repository.PublishAsync(id, actor, justification, ct);
    }
}
public sealed class MethodologyDimensionService { public void Validate(UpdateMethodologyDimensionRequest request) => MethodologyValidationService.EnsureDimension(request); }
public sealed class MethodologyConceptService { public void Validate(CreateConceptRequest request) => MethodologyValidationService.EnsureConcept(request); }
public sealed class MaturityLevelService { }
public sealed class EvidenceCriteriaService { }
public sealed class ScoringRuleService { }
public sealed class IndicatorRuleService { }
public sealed class QuestionBankService { public void Validate(CreateQuestionBankItemRequest request) => MethodologyValidationService.EnsureQuestion(request); }
public sealed class DiagnosticTemplateService { public void Validate(CreateDiagnosticTemplateRequest request) => MethodologyValidationService.EnsureTemplate(request); }
public sealed class MethodologyIndexService { }
public sealed class MethodologyQuestionBankService { }
public sealed class MethodologyPromptTemplateService { }
public sealed class MethodologyGuardrailService { }

public sealed class CreateMethodologyVersionUseCase(IMethodologyStudioRepository repository)
{
    public Task<Guid> ExecuteAsync(string code, string name, string? description, Guid? actor, CancellationToken ct) => repository.CreateDraftAsync(code, name, description, null, actor, ct);
}
public sealed class CloneMethodologyVersionUseCase(IMethodologyStudioRepository repository)
{
    public Task<Guid> ExecuteAsync(Guid source, string code, string name, Guid? actor, CancellationToken ct) => repository.CreateDraftAsync(code, name, "Versão clonada para evolução controlada.", source, actor, ct);
}
public sealed class PublishMethodologyVersionUseCase(MethodologyPublicationService service)
{
    public Task ExecuteAsync(Guid id, Guid? actor, string justification, CancellationToken ct) => service.PublishAsync(id, actor, justification, ct);
}
public sealed class ValidateMethodologyConsistencyUseCase(MethodologyValidationService service)
{
    public Task<IReadOnlyList<MethodologyValidationIssue>> ExecuteAsync(Guid id, CancellationToken ct) => service.ValidateAsync(id, ct);
}
public sealed class ImportOfficialMethodologySeedUseCase { }
public sealed class LinkQuestionToConceptUseCase { }
public sealed class LinkQuestionToIndexUseCase { }
public sealed class ResolveActiveMethodologyForOrganizationUseCase { }
