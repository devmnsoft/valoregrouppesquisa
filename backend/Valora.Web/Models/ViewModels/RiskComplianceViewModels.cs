using Valora.Application.RiskCompliance;

namespace Valora.Web.Models.ViewModels;

public sealed record RiskComplianceIndexViewModel(RiskComplianceDashboardDto Dashboard,IReadOnlyList<RiskDto> Risks,IReadOnlyList<ControlDto> Controls,IReadOnlyList<NonConformityDto> NonConformities);
public sealed record RiskComplianceSectionViewModel(string Title,string Eyebrow,string Description,IReadOnlyList<RiskDto> Risks,IReadOnlyList<ControlDto> Controls,IReadOnlyList<FrameworkDto> Frameworks,IReadOnlyList<NonConformityDto> NonConformities,IReadOnlyList<HeatmapPointDto> Heatmap);
