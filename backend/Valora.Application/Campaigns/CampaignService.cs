using Valora.Application.Contracts;
using Valora.Domain.Entities;

namespace Valora.Application.Campaigns;

public sealed class CampaignService(
    ICampaignRepository repository,
    ISurveyRepository surveys,
    IOrganizationStructureRepository? structure = null,
    IFormRepository? forms = null) : ICampaignService {

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Transitions =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase) {
            [CanonicalCampaignStatus.Draft] = new HashSet<string>([CanonicalCampaignStatus.Scheduled, CanonicalCampaignStatus.Active, CanonicalCampaignStatus.Cancelled], StringComparer.OrdinalIgnoreCase),
            [CanonicalCampaignStatus.Scheduled] = new HashSet<string>([CanonicalCampaignStatus.Active, CanonicalCampaignStatus.Cancelled], StringComparer.OrdinalIgnoreCase),
            [CanonicalCampaignStatus.Active] = new HashSet<string>([CanonicalCampaignStatus.Paused, CanonicalCampaignStatus.Closed, CanonicalCampaignStatus.Cancelled], StringComparer.OrdinalIgnoreCase),
            [CanonicalCampaignStatus.Paused] = new HashSet<string>([CanonicalCampaignStatus.Active, CanonicalCampaignStatus.Closed, CanonicalCampaignStatus.Cancelled], StringComparer.OrdinalIgnoreCase),
            [CanonicalCampaignStatus.Closed] = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            [CanonicalCampaignStatus.Cancelled] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        };

    public Task<IReadOnlyList<CampaignSummaryDto>> ListAsync(Guid organizationId, CampaignFilter? filter, CancellationToken ct) {
        RequireOrganization(organizationId);
        return repository.ListAsync(organizationId, filter, ct);
    }

    public Task<CampaignDetailDto?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken ct) {
        RequireOrganization(organizationId);
        return repository.GetByIdAsync(organizationId, id, ct);
    }

    public async Task<CampaignDetailDto> CreateAsync(Guid organizationId, Guid userId, CreateCampaignDto request, CancellationToken ct) {
        RequireOrganization(organizationId);
        RequireUser(userId);

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 180)
            throw new ArgumentException("Informe um nome de campanha válido com até 180 caracteres.");

        if (request.SurveyId == Guid.Empty)
            throw new ArgumentException("Selecione uma pesquisa válida.");

        var survey = await surveys.GetAdminAsync(organizationId, request.SurveyId);
        if (survey is null)
            throw new ArgumentException("Pesquisa não encontrada nesta organização.");

        if (request.UnitId.HasValue && structure is not null) {
            var unit = await structure.GetUnitAsync(organizationId, request.UnitId.Value, ct);
            if (unit is null || !string.Equals(unit.Status, "active", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Unidade selecionada não encontrada ou inativa nesta organização.");
        }

        if (request.DepartmentId.HasValue && structure is not null) {
            var dept = await structure.GetDepartmentAsync(organizationId, request.DepartmentId.Value, ct);
            if (dept is null || !string.Equals(dept.Status, "active", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Departamento selecionado não encontrado ou inativo nesta organização.");

            if (request.UnitId.HasValue && dept.UnitId.HasValue && dept.UnitId != request.UnitId)
                throw new ArgumentException("O departamento selecionado não pertence à unidade indicada.");
        }

        if (request.ResponseGoal < 0)
            throw new ArgumentException("A meta de respostas deve ser zero ou positiva.");

        if (request.StartsAt is not null && request.EndsAt is not null && request.EndsAt <= request.StartsAt)
            throw new ArgumentException("A data de encerramento da campanha deve ser posterior à data de início.");

        var campaign = new Campaign {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            SurveyId = request.SurveyId,
            UnitId = request.UnitId,
            DepartmentId = request.DepartmentId,
            Name = request.Name.Trim(),
            Audience = request.Audience?.Trim(),
            ResponseGoal = request.ResponseGoal,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            Status = CanonicalCampaignStatus.Draft,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await repository.CreateAsync(campaign, ct);
    }

    public async Task<CampaignDetailDto?> UpdateAsync(Guid organizationId, Guid id, UpdateCampaignDto request, CancellationToken ct) {
        RequireOrganization(organizationId);

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 180)
            throw new ArgumentException("Informe um nome de campanha válido com até 180 caracteres.");

        var existing = await repository.GetByIdAsync(organizationId, id, ct);
        if (existing is null) return null;

        if (existing.Status is CanonicalCampaignStatus.Closed or CanonicalCampaignStatus.Cancelled)
            throw new InvalidOperationException("Não é possível alterar uma campanha encerrada ou cancelada.");

        if (request.UnitId.HasValue && structure is not null) {
            var unit = await structure.GetUnitAsync(organizationId, request.UnitId.Value, ct);
            if (unit is null || !string.Equals(unit.Status, "active", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Unidade selecionada não encontrada ou inativa nesta organização.");
        }

        if (request.DepartmentId.HasValue && structure is not null) {
            var dept = await structure.GetDepartmentAsync(organizationId, request.DepartmentId.Value, ct);
            if (dept is null || !string.Equals(dept.Status, "active", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Departamento selecionado não encontrado ou inativo nesta organização.");

            if (request.UnitId.HasValue && dept.UnitId.HasValue && dept.UnitId != request.UnitId)
                throw new ArgumentException("O departamento selecionado não pertence à unidade indicada.");
        }

        if (request.ResponseGoal < 0)
            throw new ArgumentException("A meta de respostas deve ser zero ou positiva.");

        if (request.StartsAt is not null && request.EndsAt is not null && request.EndsAt <= request.StartsAt)
            throw new ArgumentException("A data de encerramento da campanha deve ser posterior à data de início.");

        var updated = new Campaign {
            Id = existing.Id,
            OrganizationId = organizationId,
            SurveyId = existing.SurveyId,
            UnitId = request.UnitId,
            DepartmentId = request.DepartmentId,
            Name = request.Name.Trim(),
            Audience = request.Audience?.Trim(),
            ResponseGoal = request.ResponseGoal,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            Status = existing.Status,
            CreatedBy = existing.CreatedBy,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        return await repository.UpdateAsync(updated, ct);
    }

    public async Task<CampaignDetailDto?> TransitionAsync(
        Guid organizationId,
        Guid id,
        string targetStatus,
        string? justification = null,
        string? correlationId = null,
        CancellationToken ct = default) {
        RequireOrganization(organizationId);

        targetStatus = targetStatus.Trim().ToLowerInvariant();
        if (!CanonicalCampaignStatus.All.Contains(targetStatus))
            throw new ArgumentException($"Estado de campanha '{targetStatus}' inválido.");

        var existing = await repository.GetByIdAsync(organizationId, id, ct);
        if (existing is null) return null;

        var currentStatus = existing.Status.ToLowerInvariant();
        if (string.Equals(currentStatus, targetStatus, StringComparison.OrdinalIgnoreCase)) {
            // Idempotência: repetição do comando sem efeitos colaterais
            return existing;
        }

        if (!Transitions.TryGetValue(currentStatus, out var allowed) || !allowed.Contains(targetStatus))
            throw new InvalidOperationException($"A transição de '{existing.Status}' para '{targetStatus}' não é permitida.");

        // Validação de prontidão para ativação ou agendamento
        if (targetStatus is CanonicalCampaignStatus.Active or CanonicalCampaignStatus.Scheduled) {
            object? survey = await surveys.GetAdminAsync(organizationId, existing.SurveyId);
            if (survey is null)
                throw new InvalidOperationException("Não é possível ativar a campanha: pesquisa vinculada não encontrada nesta organização.");

            string surveyStatus = (GetProperty(survey, "status")?.ToString() ?? string.Empty).Trim().ToLowerInvariant();
            if (surveyStatus is "draft")
                throw new InvalidOperationException("Não é possível ativar a campanha: a pesquisa vinculada ainda está em rascunho. Publique o diagnóstico antes de ativar a campanha.");

            if (surveyStatus is "closed" or "archived" or "cancelled")
                throw new InvalidOperationException("Não é possível ativar a campanha: a pesquisa vinculada está encerrada ou cancelada.");

            var expRaw = GetProperty(survey, "expires_at");
            if (expRaw is DateTime exp && exp <= DateTime.UtcNow)
                throw new InvalidOperationException("Não é possível ativar a campanha: o período de coleta da pesquisa expirou.");
            else if (expRaw is DateTimeOffset expOffset && expOffset <= DateTimeOffset.UtcNow)
                throw new InvalidOperationException("Não é possível ativar a campanha: o período de coleta da pesquisa expirou.");

            if (forms is not null) {
                var fvRaw = GetProperty(survey, "form_version_id");
                Guid? formVersionId = fvRaw is Guid fvid
                    ? fvid
                    : (Guid.TryParse(fvRaw?.ToString(), out Guid gFvid) ? gFvid : null);

                if (formVersionId is null || formVersionId == Guid.Empty)
                    throw new InvalidOperationException("Não é possível ativar a campanha: o diagnóstico não possui questionário publicado vinculado.");

                var questions = await forms.GetQuestionsAsync(formVersionId.Value);
                if (questions.Count == 0)
                    throw new InvalidOperationException("Não é possível ativar a campanha: o questionário vinculado não possui perguntas cadastradas.");
            }

            if (existing.EndsAt is not null && existing.EndsAt <= DateTimeOffset.UtcNow)
                throw new InvalidOperationException("Não é possível ativar a campanha: a data de encerramento da coleta já expirou.");

            if (existing.StartsAt is not null && existing.EndsAt is not null && existing.EndsAt <= existing.StartsAt)
                throw new InvalidOperationException("Não é possível ativar a campanha: a data de encerramento deve ser posterior à data de início.");
        }

        if (targetStatus == CanonicalCampaignStatus.Scheduled) {
            if (existing.StartsAt is null)
                throw new InvalidOperationException("Defina a data de início antes de agendar a campanha.");
            if (existing.StartsAt <= DateTimeOffset.UtcNow)
                throw new InvalidOperationException("A data de início da campanha agendada deve estar no futuro.");
        }

        var success = await repository.TransitionStatusAsync(
            organizationId, id, existing.Status, targetStatus, null, justification, correlationId, ct);
        return success ? await repository.GetByIdAsync(organizationId, id, ct) : null;
    }

    public async Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken ct) {
        RequireOrganization(organizationId);
        var existing = await repository.GetByIdAsync(organizationId, id, ct);
        if (existing is null) return false;

        return await repository.DeleteAsync(organizationId, id, ct);
    }

    private static void RequireOrganization(Guid value) {
        if (value == Guid.Empty) throw new UnauthorizedAccessException("Organização não informada.");
    }

    private static void RequireUser(Guid value) {
        if (value == Guid.Empty) throw new UnauthorizedAccessException("Usuário não identificado.");
    }

    private static object? GetProperty(object? obj, string propertyName) {
        if (obj is null) return null;
        if (obj is IDictionary<string, object?> dict) {
            var match = dict.FirstOrDefault(kvp => string.Equals(kvp.Key, propertyName, StringComparison.OrdinalIgnoreCase));
            return match.Value;
        }
        var prop = obj.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
        return prop?.GetValue(obj);
    }
}
