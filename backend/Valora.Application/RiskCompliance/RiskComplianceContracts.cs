using System.ComponentModel.DataAnnotations;

namespace Valora.Application.RiskCompliance;

public sealed record RiskDto(Guid Id,string Title,string Category,decimal Probability,decimal Impact,decimal Criticality,string Confidence,string OwnerName,string Status,int EvidenceCount,DateTime CreatedAt);
public sealed record ControlDto(Guid Id,string Name,Guid? RiskId,Guid? RequirementId,string State,string OwnerName,string? LastTestResult,DateTime CreatedAt);
public sealed record FrameworkDto(Guid Id,string Name,string Version,string Status,int RequirementCount,decimal? Adherence);
public sealed record NonConformityDto(Guid Id,string Title,string Severity,string Status,Guid? MitigationPlanId,DateTime DetectedAt);
public sealed record HeatmapPointDto(Guid RiskId,string Title,decimal Probability,decimal Impact,decimal Criticality,string Confidence);
public sealed record RiskComplianceDashboardDto(int CriticalRisks,int LowConfidenceRisks,int FragileControls,int OpenNonConformities,int OverdueMitigations,decimal? EvidenceBasedAdherence);

public sealed class CreateRiskRequest
{
    [Required,StringLength(180)] public string Title { get; init; }="";
    [Required] public Guid CategoryId { get; init; }
    [Range(1,5)] public decimal Probability { get; init; }
    [Range(1,5)] public decimal Impact { get; init; }
    [Required] public Guid OwnerUserId { get; init; }
    [Required,RegularExpression("^(identified|assessed|mitigating|accepted|closed)$")] public string Status { get; init; }="identified";
    [StringLength(2000)] public string? Description { get; init; }
}
public sealed class AssessRiskRequest
{
    [Range(1,5)] public decimal Probability { get; init; }
    [Range(1,5)] public decimal Impact { get; init; }
    [StringLength(2000)] public string? Rationale { get; init; }
    public IReadOnlyList<Guid> EvidenceIds { get; init; }=[];
}
public sealed class CreateControlRequest
{
    [Required,StringLength(180)] public string Name { get; init; }="";
    public Guid? RiskId { get; init; }
    public Guid? RequirementId { get; init; }
    [Required] public Guid OwnerUserId { get; init; }
    [Required,RegularExpression("^(designed|implemented|tested|ineffective|retired)$")] public string State { get; init; }="designed";
    [StringLength(2000)] public string? Description { get; init; }
}
public sealed class TestControlRequest
{
    [Required,RegularExpression("^(effective|partially_effective|ineffective)$")] public string Result { get; init; }="";
    [Required] public Guid ResponsibleUserId { get; init; }
    [MinLength(1)] public IReadOnlyList<Guid> EvidenceIds { get; init; }=[];
    [StringLength(2000)] public string? Notes { get; init; }
}
public sealed class ComplianceAssessmentRequest
{
    [Required] public Guid RequirementId { get; init; }
    [Required,RegularExpression("^(conformant|partially_conformant|non_conformant|not_assessed)$")] public string Result { get; init; }="not_assessed";
    public IReadOnlyList<Guid> EvidenceIds { get; init; }=[];
    [StringLength(2000)] public string? Rationale { get; init; }
}
public sealed class CreateMitigationPlanRequest
{
    [Required] public Guid NonConformityId { get; init; }
    [Required,StringLength(180)] public string Title { get; init; }="";
    [Required] public Guid ResponsibleUserId { get; init; }
    [Required] public DateTime DueAt { get; init; }
    [Required,RegularExpression("^(low|medium|high|critical)$")] public string Priority { get; init; }="medium";
}

public interface IRiskComplianceRepository
{
    Task<RiskComplianceDashboardDto> Dashboard(Guid organizationId,CancellationToken ct);
    Task<IReadOnlyList<RiskDto>> Risks(Guid organizationId,CancellationToken ct);
    Task<Guid> CreateRisk(Guid organizationId,Guid actorId,CreateRiskRequest request,CancellationToken ct);
    Task UpdateRisk(Guid organizationId,Guid id,CreateRiskRequest request,CancellationToken ct);
    Task AssessRisk(Guid organizationId,Guid id,Guid actorId,AssessRiskRequest request,CancellationToken ct);
    Task<IReadOnlyList<ControlDto>> Controls(Guid organizationId,CancellationToken ct);
    Task<Guid> CreateControl(Guid organizationId,Guid actorId,CreateControlRequest request,CancellationToken ct);
    Task TestControl(Guid organizationId,Guid id,Guid actorId,TestControlRequest request,CancellationToken ct);
    Task<IReadOnlyList<FrameworkDto>> Frameworks(Guid organizationId,CancellationToken ct);
    Task AssessCompliance(Guid organizationId,Guid actorId,ComplianceAssessmentRequest request,CancellationToken ct);
    Task<IReadOnlyList<NonConformityDto>> NonConformities(Guid organizationId,CancellationToken ct);
    Task<Guid> CreateMitigation(Guid organizationId,Guid actorId,CreateMitigationPlanRequest request,CancellationToken ct);
    Task<IReadOnlyList<HeatmapPointDto>> Heatmap(Guid organizationId,CancellationToken ct);
}
