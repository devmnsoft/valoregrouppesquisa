namespace Valora.Application.Methodology;

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
public sealed class MethodologyConceptService { }
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
