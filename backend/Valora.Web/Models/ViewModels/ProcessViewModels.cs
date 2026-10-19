using Valora.Application.Processes;

namespace Valora.Web.Models.ViewModels;

public sealed record ProcessesIndexViewModel(ProcessDashboardDto Dashboard, IReadOnlyList<ProcessSlaDto> Sla, IReadOnlyList<ProcessInsightDto> Insights);

public sealed record ProcessListViewModel(string Title, string Description, IReadOnlyList<ProcessDefinitionDto> Definitions, IReadOnlyList<ProcessInstanceDto> Instances, IReadOnlyList<ProcessSlaDto> Sla, IReadOnlyList<ProcessInsightDto> Insights, IReadOnlyList<ProcessTemplateDto> Templates);

public sealed class ProcessBuilderViewModel {
    public ProcessBuilderViewModel(
        ProcessDefinitionDto? definition,
        IReadOnlyList<ProcessStepDto> steps,
        IReadOnlyList<ProcessDefinitionDto> allDefinitions) {
        Definition = definition;
        Steps = steps.OrderBy(s => s.OrderIndex).ToList();
        AllDefinitions = allDefinitions;
    }

    public ProcessDefinitionDto? Definition { get; }
    public IReadOnlyList<ProcessStepDto> Steps { get; }
    public IReadOnlyList<ProcessDefinitionDto> AllDefinitions { get; }

    public bool HasInitialStep => Steps.Any(s => string.Equals(s.StepType, "initial", StringComparison.OrdinalIgnoreCase));
    public bool HasFinalStep => Steps.Any(s => string.Equals(s.StepType, "final", StringComparison.OrdinalIgnoreCase));
    public bool IsPublished => string.Equals(Definition?.Status, "published", StringComparison.OrdinalIgnoreCase);
    public bool CanPublish => !IsPublished && HasInitialStep && HasFinalStep;
}
