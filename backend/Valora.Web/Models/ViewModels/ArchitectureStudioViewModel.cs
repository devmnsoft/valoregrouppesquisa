using Valora.Application.OrganizationalArchitecture;
namespace Valora.Web.Models.ViewModels;
public sealed record ArchitectureStudioViewModel(string Section, ArchitectureSummary Summary, IReadOnlyList<ArchitectureUnit> Units, IReadOnlyList<ArchitecturePosition> Positions, IReadOnlyList<ResponsibilityItem> Responsibilities, IReadOnlyList<BusinessProcess> Processes, IReadOnlyList<DecisionRight> Decisions, IReadOnlyList<OrganizationalDependency> Dependencies, IReadOnlyList<ArchitectureRisk> Risks, string? Message=null);
