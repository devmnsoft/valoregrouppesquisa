using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace Valora.Application.Onboarding;

public sealed record OnboardingStepDto(Guid Id, string Code, string Title, string Guidance, string ActionUrl,
    bool IsRequired, string Status, DateTimeOffset? CompletedAt, decimal ProgressPercentage);
public sealed record OnboardingProgressDto(decimal Percentage, int Completed, int Total, string Status,
    IReadOnlyList<OnboardingStepDto> Steps);
public sealed record ChecklistItemDto(Guid Id, string Code, string Title, bool IsRequired, bool IsCompleted);
public sealed record AdoptionMetricDto(string FeatureCode, long Events, long ActiveUsers, DateTimeOffset? LastUsedAt);
public sealed record CustomerHealthScoreDto(decimal Score, string Level, decimal Usage, decimal Adoption,
    decimal Diagnostics, decimal Engagement, string Risk, DateTimeOffset CalculatedAt);
public sealed record CompleteOnboardingStepRequest([property: StringLength(1000)] string? Evidence);
public sealed record CreateCustomerSuccessTaskRequest(
    [property: Required, StringLength(180, MinimumLength = 3)] string Title,
    [property: StringLength(2000)] string? Description, DateTimeOffset? DueAt);
public sealed record CreateCustomerSuccessNoteRequest(
    [property: Required, StringLength(4000, MinimumLength = 3)] string Content,
    [property: StringLength(50)] string Type = "internal");

public interface IOnboardingRepository
{
    Task EnsureDefaultFlowAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OnboardingStepDto>> GetStepsAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<bool> SetStepStatusAsync(Guid organizationId, Guid userId, Guid stepId, string status, string? evidence, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChecklistItemDto>> GetChecklistAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<bool> CompleteChecklistItemAsync(Guid organizationId, Guid userId, Guid itemId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdoptionMetricDto>> GetAdoptionAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<CustomerHealthScoreDto> CalculateHealthAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<Guid> CreateTaskAsync(Guid organizationId, Guid userId, CreateCustomerSuccessTaskRequest request, CancellationToken cancellationToken);
    Task<Guid> CreateNoteAsync(Guid organizationId, Guid userId, CreateCustomerSuccessNoteRequest request, CancellationToken cancellationToken);
}

public sealed class OnboardingFlowService(IOnboardingRepository repository, ILogger<OnboardingFlowService> logger)
{
    public async Task<IReadOnlyList<OnboardingStepDto>> GetAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        RequireOrganization(organizationId);
        await repository.EnsureDefaultFlowAsync(organizationId, cancellationToken);
        logger.LogInformation("Onboarding flow loaded for organization {OrganizationId}", organizationId);
        return await repository.GetStepsAsync(organizationId, cancellationToken);
    }
    internal static void RequireOrganization(Guid id) { if (id == Guid.Empty) throw new ValidationException("Selecione uma organização válida."); }
}

public sealed class OnboardingProgressService(IOnboardingRepository repository, OnboardingFlowService flow, ILogger<OnboardingProgressService> logger)
{
    public async Task<OnboardingProgressDto> GetAsync(Guid organizationId, CancellationToken ct)
    {
        var steps = await flow.GetAsync(organizationId, ct); var done = steps.Count(x => x.Status is "completed" or "skipped");
        var percentage = steps.Count == 0 ? 0 : Math.Round(done * 100m / steps.Count, 2);
        return new(percentage, done, steps.Count, done == steps.Count && steps.Count > 0 ? "completed" : "in_progress", steps);
    }
    public async Task CompleteAsync(Guid organizationId, Guid userId, Guid stepId, string? evidence, CancellationToken ct)
    {
        Validate(organizationId, userId, stepId); var steps = await flow.GetAsync(organizationId, ct);
        var step = steps.SingleOrDefault(x => x.Id == stepId) ?? throw new ValidationException("Etapa não encontrada nesta organização.");
        var priorRequired = steps.TakeWhile(x => x.Id != step.Id).Any(x => x.IsRequired && x.Status != "completed");
        if (priorRequired) throw new ValidationException("Conclua primeiro as etapas obrigatórias anteriores.");
        if (!await repository.SetStepStatusAsync(organizationId, userId, stepId, "completed", evidence?.Trim(), ct)) throw new ValidationException("Não foi possível concluir a etapa.");
        logger.LogInformation("Onboarding step {StepId} completed for organization {OrganizationId}", stepId, organizationId);
    }
    public async Task SkipAsync(Guid organizationId, Guid userId, Guid stepId, CancellationToken ct)
    {
        Validate(organizationId, userId, stepId); var step = (await flow.GetAsync(organizationId, ct)).SingleOrDefault(x => x.Id == stepId) ?? throw new ValidationException("Etapa não encontrada nesta organização.");
        if (step.IsRequired) throw new ValidationException("Etapas obrigatórias não podem ser puladas.");
        await repository.SetStepStatusAsync(organizationId, userId, stepId, "skipped", null, ct);
    }
    private static void Validate(Guid organizationId, Guid userId, Guid stepId) { OnboardingFlowService.RequireOrganization(organizationId); if (userId == Guid.Empty || stepId == Guid.Empty) throw new ValidationException("Usuário e etapa devem ser válidos."); }
}

public sealed class OnboardingChecklistService(IOnboardingRepository repository)
{
    public Task<IReadOnlyList<ChecklistItemDto>> GetAsync(Guid organizationId, CancellationToken ct) { OnboardingFlowService.RequireOrganization(organizationId); return repository.GetChecklistAsync(organizationId, ct); }
    public async Task CompleteAsync(Guid organizationId, Guid userId, Guid itemId, CancellationToken ct) { OnboardingFlowService.RequireOrganization(organizationId); if (userId == Guid.Empty || itemId == Guid.Empty || !await repository.CompleteChecklistItemAsync(organizationId, userId, itemId, ct)) throw new ValidationException("Item não encontrado nesta organização."); }
}
public sealed class CustomerAdoptionService(IOnboardingRepository repository) { public Task<IReadOnlyList<AdoptionMetricDto>> GetAsync(Guid organizationId, CancellationToken ct) { OnboardingFlowService.RequireOrganization(organizationId); return repository.GetAdoptionAsync(organizationId, ct); } }
public sealed class CustomerHealthScoreService(IOnboardingRepository repository) { public Task<CustomerHealthScoreDto> GetAsync(Guid organizationId, CancellationToken ct) { OnboardingFlowService.RequireOrganization(organizationId); return repository.CalculateHealthAsync(organizationId, ct); } }
public sealed class CustomerSuccessService(IOnboardingRepository repository)
{
    public Task<Guid> CreateTaskAsync(Guid organizationId, Guid userId, CreateCustomerSuccessTaskRequest request, CancellationToken ct) { Validate(organizationId, userId); return repository.CreateTaskAsync(organizationId, userId, request, ct); }
    public Task<Guid> CreateNoteAsync(Guid organizationId, Guid userId, CreateCustomerSuccessNoteRequest request, CancellationToken ct) { Validate(organizationId, userId); return repository.CreateNoteAsync(organizationId, userId, request, ct); }
    private static void Validate(Guid organizationId, Guid userId) { OnboardingFlowService.RequireOrganization(organizationId); if (userId == Guid.Empty) throw new ValidationException("Usuário autenticado inválido."); }
}
