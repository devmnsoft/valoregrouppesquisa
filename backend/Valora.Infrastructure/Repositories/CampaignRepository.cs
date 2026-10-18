using System.Text.Json;
using Dapper;
using Valora.Application.Campaigns;
using Valora.Application.Contracts;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Repositories;

public sealed class CampaignRepository(IDbConnectionFactory connections) : ICampaignRepository {
    private const string BaseSelect = """
        SELECT c.id AS "Id",
               c.organization_id AS "OrganizationId",
               c.survey_id AS "SurveyId",
               COALESCE(s.title, s.name, 'Pesquisa') AS "SurveyTitle",
               s.status AS "SurveyStatus",
               s.expires_at AS "SurveyExpiresAt",
               c.unit_id AS "UnitId",
               u.name AS "UnitName",
               c.department_id AS "DepartmentId",
               d.name AS "DepartmentName",
               c.name AS "Name",
               c.audience AS "Audience",
               c.response_goal AS "ResponseGoal",
               (
                   SELECT COUNT(*)::int
                     FROM valorapesquisa.responses r
                    WHERE r.organization_id = c.organization_id
                      AND r.survey_id = c.survey_id
                      AND (c.starts_at IS NULL OR r.created_at >= c.starts_at)
                      AND (c.ends_at IS NULL OR r.created_at <= c.ends_at)
                      AND r.is_deleted = false
                      AND r.status IN ('completed', 'submitted')
               ) AS "ActualResponses",
               c.starts_at AS "StartsAt",
               c.ends_at AS "EndsAt",
               c.status AS "Status",
               (
                   SELECT sl.public_url
                     FROM valorapesquisa.survey_links sl
                    WHERE sl.organization_id = c.organization_id
                      AND sl.survey_id = c.survey_id
                      AND sl.is_deleted = false
                      AND sl.status = 'active'
                      AND sl.revoked_at IS NULL
                      AND (sl.starts_at IS NULL OR sl.starts_at <= now())
                      AND (sl.expires_at IS NULL OR sl.expires_at > now())
                    ORDER BY sl.created_at DESC
                    LIMIT 1
               ) AS "ConfiguredPublicUrl",
               (
                   SELECT sl.status
                     FROM valorapesquisa.survey_links sl
                    WHERE sl.organization_id = c.organization_id
                      AND sl.survey_id = c.survey_id
                      AND sl.is_deleted = false
                    ORDER BY sl.created_at DESC
                    LIMIT 1
               ) AS "LatestLinkStatus",
               c.created_by AS "CreatedBy",
               usr.name AS "CreatedByName",
               c.reference_code AS "ReferenceCode",
               c.created_at AS "CreatedAt",
               c.updated_at AS "UpdatedAt"
          FROM valorapesquisa.campaigns c
          JOIN valorapesquisa.surveys s ON s.id = c.survey_id
          LEFT JOIN valorapesquisa.units u ON u.id = c.unit_id AND u.organization_id = c.organization_id AND u.deleted_at IS NULL
          LEFT JOIN valorapesquisa.departments d ON d.id = c.department_id AND d.organization_id = c.organization_id AND d.deleted_at IS NULL
          LEFT JOIN valorapesquisa.users usr ON usr.id = c.created_by
         WHERE c.organization_id = @organizationId
           AND c.deleted_at IS NULL
        """;

    public async Task<IReadOnlyList<CampaignSummaryDto>> ListAsync(Guid organizationId, CampaignFilter? filter, CancellationToken ct) {
        using var connection = connections.Create();
        var sql = BaseSelect;

        if (filter?.SurveyId is not null) sql += " AND c.survey_id = @surveyId";
        if (filter?.UnitId is not null) sql += " AND c.unit_id = @unitId";
        if (filter?.DepartmentId is not null) sql += " AND c.department_id = @departmentId";
        if (!string.IsNullOrWhiteSpace(filter?.Status)) sql += " AND c.status = @status";

        sql += " ORDER BY c.created_at DESC";

        var rows = (await connection.QueryAsync<CampaignRow>(new CommandDefinition(
            sql,
            new {
                organizationId,
                surveyId = filter?.SurveyId,
                unitId = filter?.UnitId,
                departmentId = filter?.DepartmentId,
                status = filter?.Status
            },
            cancellationToken: ct))).AsList();

        return rows.Select(MapToSummary).ToList();
    }

    public async Task<CampaignDetailDto?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken ct) {
        using var connection = connections.Create();
        var sql = BaseSelect + " AND c.id = @id";

        var row = await connection.QuerySingleOrDefaultAsync<CampaignRow>(new CommandDefinition(
            sql, new { organizationId, id }, cancellationToken: ct));

        return row is null ? null : MapToDetail(row);
    }

    public async Task<CampaignDetailDto> CreateAsync(Campaign campaign, CancellationToken ct) {
        using var connection = connections.Create();
        const string insertSql = """
            INSERT INTO valorapesquisa.campaigns (
                id, organization_id, survey_id, unit_id, department_id,
                name, audience, response_goal, starts_at, ends_at,
                public_token_hash, status, created_by, created_at, updated_at
            ) VALUES (
                @Id, @OrganizationId, @SurveyId, @UnitId, @DepartmentId,
                @Name, @Audience, @ResponseGoal, @StartsAt, @EndsAt,
                @PublicTokenHash, @Status, @CreatedBy, @CreatedAt, @UpdatedAt
            );
            """;

        await connection.ExecuteAsync(new CommandDefinition(insertSql, campaign, cancellationToken: ct));
        return (await GetByIdAsync(campaign.OrganizationId, campaign.Id, ct))!;
    }

    public async Task<CampaignDetailDto?> UpdateAsync(Campaign campaign, CancellationToken ct) {
        using var connection = connections.Create();
        const string updateSql = """
            UPDATE valorapesquisa.campaigns
               SET name = @Name,
                   unit_id = @UnitId,
                   department_id = @DepartmentId,
                   audience = @Audience,
                   response_goal = @ResponseGoal,
                   starts_at = @StartsAt,
                   ends_at = @EndsAt,
                   updated_at = now()
             WHERE id = @Id
               AND organization_id = @OrganizationId
               AND deleted_at IS NULL;
            """;

        var affected = await connection.ExecuteAsync(new CommandDefinition(updateSql, campaign, cancellationToken: ct));
        return affected > 0 ? await GetByIdAsync(campaign.OrganizationId, campaign.Id, ct) : null;
    }

    public async Task<bool> TransitionStatusAsync(
        Guid organizationId,
        Guid id,
        string expectedStatus,
        string newStatus,
        Guid? userId,
        string? justification,
        string? correlationId,
        CancellationToken ct) {
        using var connection = connections.Create();

        const string updateSql = """
            UPDATE valorapesquisa.campaigns
               SET status = @newStatus,
                   updated_at = now()
             WHERE id = @id
               AND organization_id = @organizationId
               AND status = @expectedStatus
               AND deleted_at IS NULL;
            """;

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            updateSql, new { organizationId, id, expectedStatus, newStatus }, cancellationToken: ct));

        if (affected == 0) {
            // Verificar idempotência vs concorrência
            const string verifySql = """
                SELECT status
                  FROM valorapesquisa.campaigns
                 WHERE id = @id AND organization_id = @organizationId AND deleted_at IS NULL;
                """;
            var currentStatus = await connection.QuerySingleOrDefaultAsync<string>(
                new CommandDefinition(verifySql, new { organizationId, id }, cancellationToken: ct));

            if (currentStatus is null) return false;
            if (string.Equals(currentStatus, newStatus, StringComparison.OrdinalIgnoreCase)) {
                // Idempotência: repetição do comando já no estado desejado sem erro nem duplicidade
                return true;
            }

            throw new InvalidOperationException(
                $"Conflito de concorrência: a campanha encontra-se no estado '{currentStatus}', e não '{expectedStatus}'. Atualize os dados e tente novamente.");
        }

        // Registrar evento de governança e histórico
        try {
            var campaign = await GetByIdAsync(organizationId, id, ct);
            if (campaign is not null) {
                var eventData = new {
                    campaignId = id,
                    surveyId = campaign.SurveyId,
                    previousStatus = expectedStatus,
                    newStatus,
                    userId,
                    justification,
                    timestamp = DateTime.UtcNow
                };

                const string recordEventSql = """
                    INSERT INTO valorapesquisa.platform_governance_events (
                        id, organization_id, user_id, action, entity_type, entity_id, survey_id,
                        status, reason, metadata_json, data, correlation_id, created_at
                    ) VALUES (
                        gen_random_uuid(), @organizationId, @userId, @action, 'campaign', @id, @surveyId,
                        @newStatus, @justification, '{}'::jsonb, @dataJson::jsonb, @correlationId, now()
                    );
                    """;

                await connection.ExecuteAsync(new CommandDefinition(
                    recordEventSql,
                    new {
                        organizationId,
                        userId,
                        action = $"campaign.{newStatus}",
                        id,
                        surveyId = campaign.SurveyId,
                        newStatus,
                        justification,
                        dataJson = JsonSerializer.Serialize(eventData),
                        correlationId
                    },
                    cancellationToken: ct));
            }
        }
        catch {
            // Auditoria não bloqueia transição concluída
        }

        return true;
    }

    public async Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken ct) {
        using var connection = connections.Create();

        // Verificar se já possui respostas coletadas
        const string checkResponsesSql = """
            SELECT (
                SELECT COUNT(*)::int
                  FROM valorapesquisa.responses r
                 WHERE r.organization_id = c.organization_id
                   AND r.survey_id = c.survey_id
                   AND (c.starts_at IS NULL OR r.created_at >= c.starts_at)
                   AND (c.ends_at IS NULL OR r.created_at <= c.ends_at)
                   AND r.is_deleted = false
                   AND r.status IN ('completed', 'submitted')
            )
            FROM valorapesquisa.campaigns c
            WHERE c.id = @id AND c.organization_id = @organizationId AND c.deleted_at IS NULL;
            """;

        var responsesCount = await connection.QuerySingleOrDefaultAsync<int?>(
            new CommandDefinition(checkResponsesSql, new { organizationId, id }, cancellationToken: ct));

        if (responsesCount is not null && responsesCount.Value > 0) {
            throw new InvalidOperationException(
                "A campanha já possui respostas registradas e não pode ser excluída. Encerre ou cancele a campanha para preservar o histórico operacional.");
        }

        const string deleteSql = """
            UPDATE valorapesquisa.campaigns
               SET deleted_at = now(),
                   updated_at = now()
             WHERE id = @id
               AND organization_id = @organizationId
               AND deleted_at IS NULL;
            """;

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            deleteSql, new { organizationId, id }, cancellationToken: ct));
        return affected > 0;
    }

    private static decimal CalculateRate(int actual, int goal) {
        if (goal <= 0) return 0m;
        return Math.Round((decimal)actual / goal * 100m, 1);
    }

    private static (string? Url, string Status, string? Reason, string? Action) ResolveLink(CampaignRow r) {
        var surveyStatus = (r.SurveyStatus ?? string.Empty).Trim().ToLowerInvariant();

        if (surveyStatus is "draft") {
            return (null, "draft", "A pesquisa vinculada ainda está em rascunho.", "Publique o diagnóstico em Pesquisas para liberar a coleta pública.");
        }

        if (surveyStatus is "closed" or "archived" or "cancelled") {
            return (null, surveyStatus, "A pesquisa vinculada está encerrada ou cancelada.", "Reative a pesquisa para receber respostas.");
        }

        if (r.SurveyExpiresAt is not null && r.SurveyExpiresAt <= DateTimeOffset.UtcNow) {
            return (null, "expired", "O período de coleta do diagnóstico já expirou.", "Ajuste a data de término nas configurações da pesquisa.");
        }

        // Pesquisa ativa / publicada
        var canonicalPath = !string.IsNullOrWhiteSpace(r.ConfiguredPublicUrl)
            ? r.ConfiguredPublicUrl
            : $"/public/surveys/{r.SurveyId}";

        return (canonicalPath, "active", null, null);
    }

    private static CampaignSummaryDto MapToSummary(CampaignRow r) {
        var link = ResolveLink(r);
        return new(r.Id, r.OrganizationId, r.SurveyId, r.SurveyTitle, r.UnitId, r.UnitName,
            r.DepartmentId, r.DepartmentName, r.Name, r.Audience, r.ResponseGoal,
            r.ActualResponses, CalculateRate(r.ActualResponses, r.ResponseGoal),
            r.StartsAt, r.EndsAt, r.Status, link.Url,
            r.CreatedAt, r.UpdatedAt, link.Status, link.Reason, link.Action,
            r.ReferenceCode);
    }

    private static CampaignDetailDto MapToDetail(CampaignRow r) {
        var link = ResolveLink(r);
        return new(r.Id, r.OrganizationId, r.SurveyId, r.SurveyTitle, r.UnitId, r.UnitName,
            r.DepartmentId, r.DepartmentName, r.Name, r.Audience, r.ResponseGoal,
            r.ActualResponses, CalculateRate(r.ActualResponses, r.ResponseGoal),
            r.StartsAt, r.EndsAt, r.Status, link.Url,
            r.CreatedBy, r.CreatedByName, r.CreatedAt, r.UpdatedAt,
            link.Status, link.Reason, link.Action,
            r.ReferenceCode);
    }

    private sealed record CampaignRow {
        public Guid Id { get; init; }
        public Guid OrganizationId { get; init; }
        public Guid SurveyId { get; init; }
        public string SurveyTitle { get; init; } = string.Empty;
        public string? SurveyStatus { get; init; }
        public DateTimeOffset? SurveyExpiresAt { get; init; }
        public Guid? UnitId { get; init; }
        public string? UnitName { get; init; }
        public Guid? DepartmentId { get; init; }
        public string? DepartmentName { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Audience { get; init; }
        public int ResponseGoal { get; init; }
        public int ActualResponses { get; init; }
        public DateTimeOffset? StartsAt { get; init; }
        public DateTimeOffset? EndsAt { get; init; }
        public string Status { get; init; } = string.Empty;
        public string? ConfiguredPublicUrl { get; init; }
        public string? LatestLinkStatus { get; init; }
        public Guid? CreatedBy { get; init; }
        public string? CreatedByName { get; init; }
        public long? ReferenceCode { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}

