using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Journey;

namespace Valora.Application.RiskCompliance;

internal static class RiskComplianceGuard
{
    public static void Organization(Guid id){if(id==Guid.Empty)throw new InvalidOperationException("Selecione uma organização para continuar.");}
    public static void Actor(Guid id){if(id==Guid.Empty)throw new InvalidOperationException("Não foi possível identificar o responsável pela operação.");}
}

public sealed class RiskRegisterService(IRiskComplianceRepository repository,ILogger<RiskRegisterService> logger)
{
    public Task<RiskComplianceDashboardDto> Dashboard(Guid o,CancellationToken ct){RiskComplianceGuard.Organization(o);return repository.Dashboard(o,ct);}
    public Task<IReadOnlyList<RiskDto>> List(Guid o,CancellationToken ct){RiskComplianceGuard.Organization(o);return repository.Risks(o,ct);}
    public async Task<Guid> Create(Guid o,Guid actor,CreateRiskRequest r,CancellationToken ct){RiskComplianceGuard.Organization(o);RiskComplianceGuard.Actor(actor);Validate(r);var id=await repository.CreateRisk(o,actor,r,ct);logger.LogInformation("risk.created {RiskId} for organization {OrganizationId}",id,o);return id;}
    public Task Update(Guid o,Guid id,CreateRiskRequest r,CancellationToken ct){RiskComplianceGuard.Organization(o);if(id==Guid.Empty)throw new ArgumentException("Risco inválido.");Validate(r);return repository.UpdateRisk(o,id,r,ct);}
    private static void Validate(CreateRiskRequest r){if(r.CategoryId==Guid.Empty||r.OwnerUserId==Guid.Empty)throw new ArgumentException("Categoria e responsável são obrigatórios.");if(r.Probability is <1 or >5||r.Impact is <1 or >5)throw new ArgumentException("Probabilidade e impacto devem estar entre 1 e 5.");}
}
public sealed class RiskAssessmentService(IRiskComplianceRepository repository,ILogger<RiskAssessmentService> logger)
{
    public async Task Assess(Guid o,Guid id,Guid actor,AssessRiskRequest r,CancellationToken ct){RiskComplianceGuard.Organization(o);RiskComplianceGuard.Actor(actor);if(r.Probability is <1 or >5||r.Impact is <1 or >5)throw new ArgumentException("Probabilidade e impacto devem estar entre 1 e 5.");await repository.AssessRisk(o,id,actor,r,ct);logger.LogInformation("risk.assessed {RiskId}; evidence count {EvidenceCount}",id,r.EvidenceIds.Count);}
}
public sealed class RiskControlService(IRiskComplianceRepository repository,ILogger<RiskControlService> logger)
{
    public Task<IReadOnlyList<ControlDto>> List(Guid o,CancellationToken ct){RiskComplianceGuard.Organization(o);return repository.Controls(o,ct);}
    public async Task<Guid> Create(Guid o,Guid actor,CreateControlRequest r,CancellationToken ct){RiskComplianceGuard.Organization(o);RiskComplianceGuard.Actor(actor);if((r.RiskId is null)==(r.RequirementId is null))throw new ArgumentException("Vincule o controle a um risco ou a um requisito de conformidade.");var id=await repository.CreateControl(o,actor,r,ct);logger.LogInformation("control.created {ControlId}",id);return id;}
    public Task Test(Guid o,Guid id,Guid actor,TestControlRequest r,CancellationToken ct){RiskComplianceGuard.Organization(o);RiskComplianceGuard.Actor(actor);if(r.ResponsibleUserId==Guid.Empty||r.EvidenceIds.Count==0)throw new ArgumentException("Teste de controle exige responsável e evidência.");return repository.TestControl(o,id,actor,r,ct);}
}
public sealed class ComplianceFrameworkService(IRiskComplianceRepository repository){public Task<IReadOnlyList<FrameworkDto>> List(Guid o,CancellationToken ct){RiskComplianceGuard.Organization(o);return repository.Frameworks(o,ct);}}
public sealed class ComplianceAssessmentService(IRiskComplianceRepository repository,ILogger<ComplianceAssessmentService> logger)
{
    public async Task Assess(Guid o,Guid actor,ComplianceAssessmentRequest r,CancellationToken ct){RiskComplianceGuard.Organization(o);RiskComplianceGuard.Actor(actor);if(r.RequirementId==Guid.Empty)throw new ArgumentException("Requisito obrigatório.");if(r.Result!="not_assessed"&&r.EvidenceIds.Count==0)throw new ArgumentException("Não é possível afirmar conformidade sem evidência.");await repository.AssessCompliance(o,actor,r,ct);logger.LogInformation("compliance.assessed {RequirementId}",r.RequirementId);}
}
public sealed class NonConformityService(IRiskComplianceRepository repository){public Task<IReadOnlyList<NonConformityDto>> List(Guid o,CancellationToken ct){RiskComplianceGuard.Organization(o);return repository.NonConformities(o,ct);}}
public sealed class MitigationPlanService(IRiskComplianceRepository repository,ILogger<MitigationPlanService> logger)
{
    public async Task<Guid> Create(Guid o,Guid actor,CreateMitigationPlanRequest r,CancellationToken ct){RiskComplianceGuard.Organization(o);RiskComplianceGuard.Actor(actor);if(r.NonConformityId==Guid.Empty||r.ResponsibleUserId==Guid.Empty||r.DueAt<=DateTime.UtcNow)throw new ArgumentException("Informe não conformidade, responsável e prazo futuro.");var id=await repository.CreateMitigation(o,actor,r,ct);logger.LogInformation("mitigation_plan.created {PlanId}",id);return id;}
}
public sealed class AuditReviewService(IJourneyRepository journey,IAuditRepository audit,ILogger<AuditReviewService> logger)
{
    public async Task RecordCompletion(Guid organizationId,Guid reviewId,Guid actorId,string evidenceSummary,CancellationToken ct)
    {
        RiskComplianceGuard.Organization(organizationId);RiskComplianceGuard.Actor(actorId);
        if(reviewId==Guid.Empty||string.IsNullOrWhiteSpace(evidenceSummary))throw new ArgumentException("A conclusão da auditoria exige revisão e resumo das evidências.");
        await journey.Register(organizationId,actorId,new RegisterJourneyEventRequest(null,null,null,"audit_review.completed","Revisão de auditoria concluída","Conclusão registrada após validação humana.","audit_review",reviewId,"high",null,null,evidenceSummary,DateTime.UtcNow),ct);
        await audit.AddAsync(new AuditEntry(organizationId,actorId,"audit_review.completed","audit_review",reviewId.ToString(),"Revisão de auditoria concluída com evidências.","{}",module:"risk_compliance"));
        logger.LogInformation("audit_review.completed {ReviewId} for {OrganizationId}",reviewId,organizationId);
    }
}
public sealed class RiskHeatmapService(IRiskComplianceRepository repository){public Task<IReadOnlyList<HeatmapPointDto>> Get(Guid o,CancellationToken ct){RiskComplianceGuard.Organization(o);return repository.Heatmap(o,ct);}}
