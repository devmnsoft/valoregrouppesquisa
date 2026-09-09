using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace Valora.Application.ModularSaas;

public sealed class CommercialSaasService(ICommercialSaasRepository repository, ILogger<CommercialSaasService> logger) {
    private static readonly IReadOnlySet<string> ModuleStatuses =
        new HashSet<string>(["active", "read_only", "suspended", "expired", "cancelled"], StringComparer.OrdinalIgnoreCase);

    public Task<IReadOnlyList<CommercialModule>> ListModulesAsync(Guid? clientId, CancellationToken cancellationToken = default) =>
        repository.ListModulesAsync(clientId is { } id && id != Guid.Empty ? id : null, cancellationToken);

    public Task<IReadOnlyList<CommercialPlan>> ListPlansAsync(CancellationToken cancellationToken = default) =>
        repository.ListPlansAsync(cancellationToken);

    public Task<ModuleAccessDecision> EvaluateAccessAsync(Guid clientId, string moduleCode, bool writeOperation,
        CancellationToken cancellationToken = default) {
        if (clientId == Guid.Empty)
            return Task.FromResult(ModuleAccessDecision.Denied("CLIENT_CONTEXT_REQUIRED", "Selecione um cliente para operar esta área."));
        return repository.EvaluateAccessAsync(clientId, NormalizeModuleCode(moduleCode), writeOperation, cancellationToken);
    }

    public async Task SetModuleStatusAsync(Guid clientId, string moduleCode, string status, Guid actorUserId,
        string reason, string correlationId, CancellationToken cancellationToken = default) {
        ValidateIdentity(clientId, actorUserId);
        var normalizedStatus = status.Trim().ToLowerInvariant();
        if (!ModuleStatuses.Contains(normalizedStatus)) throw new ValidationException("Selecione um status de contratação válido.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10)
            throw new ValidationException("Informe um motivo com pelo menos 10 caracteres.");
        var normalizedModule = NormalizeModuleCode(moduleCode);
        if (!await repository.SetModuleStatusAsync(clientId, normalizedModule, normalizedStatus, actorUserId,
                reason.Trim(), correlationId, cancellationToken))
            throw new InvalidOperationException("Cliente, assinatura ou módulo não encontrado.");
        logger.LogInformation(
            "Module contract updated. ClientId={ClientId} UserId={UserId} ModuleCode={ModuleCode} Status={Status} CorrelationId={CorrelationId}",
            clientId, actorUserId, normalizedModule, normalizedStatus, correlationId);
    }

    public async Task RequestUpgradeAsync(Guid clientId, Guid actorUserId, string moduleCode, string reason,
        string correlationId, CancellationToken cancellationToken = default) {
        ValidateIdentity(clientId, actorUserId);
        if (string.IsNullOrWhiteSpace(reason)) throw new ValidationException("Informe como este módulo será utilizado.");
        var normalizedModule = NormalizeModuleCode(moduleCode);
        await repository.RequestUpgradeAsync(clientId, actorUserId, normalizedModule, reason.Trim(), correlationId, cancellationToken);
        logger.LogInformation(
            "Module upgrade requested. ClientId={ClientId} UserId={UserId} ModuleCode={ModuleCode} CorrelationId={CorrelationId}",
            clientId, actorUserId, normalizedModule, correlationId);
    }

    private static string NormalizeModuleCode(string value) {
        if (string.IsNullOrWhiteSpace(value)) throw new ValidationException("Selecione um módulo.");
        var normalized = value.Trim().ToLowerInvariant().Replace('-', '_');
        if (normalized.Any(character => !char.IsLetterOrDigit(character) && character != '_'))
            throw new ValidationException("Código de módulo inválido.");
        return normalized;
    }

    private static void ValidateIdentity(Guid clientId, Guid actorUserId) {
        if (clientId == Guid.Empty) throw new ValidationException("Selecione um cliente para continuar.");
        if (actorUserId == Guid.Empty) throw new ValidationException("Usuário autenticado inválido.");
    }
}
