using Valora.Application.Processes;

namespace Valora.Web.Models.ViewModels;

public sealed record ProcessesIndexViewModel(ProcessDashboardDto Dashboard,IReadOnlyList<ProcessSlaDto> Sla,IReadOnlyList<ProcessInsightDto> Insights);
public sealed record ProcessListViewModel(string Title,string Description,IReadOnlyList<ProcessDefinitionDto> Definitions,IReadOnlyList<ProcessInstanceDto> Instances,IReadOnlyList<ProcessSlaDto> Sla,IReadOnlyList<ProcessInsightDto> Insights,IReadOnlyList<ProcessTemplateDto> Templates);
