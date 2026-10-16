using Valora.Application.Contracts;
using Valora.Domain.Entities;

namespace Valora.Application.Campaigns;

public sealed class CampaignService(
    ICampaignRepository repository,
    ISurveyRepository surveys) : ICampaignService {

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

    public async Task<CampaignDetailDto?> TransitionAsync(Guid organizationId, Guid id, string targetStatus, string? justification, CancellationToken ct) {
        RequireOrganization(organizationId);

        targetStatus = targetStatus.Trim().ToLowerInvariant();
        if (!CanonicalCampaignStatus.All.Contains(targetStatus))
            throw new ArgumentException($"Estado de campanha '{targetStatus}' inválido.");

        var existing = await repository.GetByIdAsync(organizationId, id, ct);
        if (existing is null) return null;

        var currentStatus = existing.Status.ToLowerInvariant();
        if (!Transitions.TryGetValue(currentStatus, out var allowed) || !allowed.Contains(targetStatus))
            throw new InvalidOperationException($"A transição de '{existing.Status}' para '{targetStatus}' não é permitida.");

        if (targetStatus == CanonicalCampaignStatus.Scheduled) {
            if (existing.StartsAt is null)
                throw new InvalidOperationException("Defina a data de início antes de agendar a campanha.");
            if (existing.StartsAt <= DateTimeOffset.UtcNow)
                throw new InvalidOperationException("A data de início da campanha agendada deve estar no futuro.");
        }

        var success = await repository.TransitionStatusAsync(organizationId, id, targetStatus, ct);
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
}
