namespace Valora.Application.ModularSaas;

public interface ICommercialSaasRepository
{
    Task<IReadOnlyList<CommercialModule>> ListModulesAsync(Guid? clientId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CommercialPlan>> ListPlansAsync(CancellationToken cancellationToken);
    Task<ModuleAccessDecision> EvaluateAccessAsync(Guid clientId, string moduleCode, bool writeOperation, CancellationToken cancellationToken);
    Task<bool> SetModuleStatusAsync(Guid clientId, string moduleCode, string status, Guid actorUserId,
        string reason, string correlationId, CancellationToken cancellationToken);
    Task RequestUpgradeAsync(Guid clientId, Guid actorUserId, string moduleCode, string reason,
        string correlationId, CancellationToken cancellationToken);
}
