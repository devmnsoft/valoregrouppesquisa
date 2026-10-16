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
                      AND (c.unit_id IS NULL OR r.unit_id = c.unit_id)
                      AND (c.department_id IS NULL OR r.department_id = c.department_id)
                      AND (c.starts_at IS NULL OR r.created_at >= c.starts_at)
                      AND (c.ends_at IS NULL OR r.created_at <= c.ends_at)
                      AND r.status IN ('completed', 'submitted')
               ) AS "ActualResponses",
               c.starts_at AS "StartsAt",
               c.ends_at AS "EndsAt",
               c.status AS "Status",
               (
                   SELECT sl.public_token
                     FROM valorapesquisa.survey_links sl
                    WHERE sl.survey_id = c.survey_id
                      AND sl.is_revoked = false
                    LIMIT 1
               ) AS "PublicToken",
               c.created_by AS "CreatedBy",
               usr.name AS "CreatedByName",
               c.created_at AS "CreatedAt",
               c.updated_at AS "UpdatedAt"
          FROM valorapesquisa.campaigns c
          JOIN valorapesquisa.surveys s ON s.id = c.survey_id
          LEFT JOIN valorapesquisa.units u ON u.id = c.unit_id
          LEFT JOIN valorapesquisa.departments d ON d.id = c.department_id
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

    public async Task<bool> TransitionStatusAsync(Guid organizationId, Guid id, string newStatus, CancellationToken ct) {
        using var connection = connections.Create();
        const string updateSql = """
            UPDATE valorapesquisa.campaigns
               SET status = @newStatus,
                   updated_at = now()
             WHERE id = @id
               AND organization_id = @organizationId
               AND deleted_at IS NULL;
            """;

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            updateSql, new { organizationId, id, newStatus }, cancellationToken: ct));
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken ct) {
        using var connection = connections.Create();
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
        return Math.Round(Math.Min(100m, (decimal)actual / goal * 100m), 1);
    }

    private static string? FormatPublicUrl(string? token) =>
        string.IsNullOrWhiteSpace(token) ? null : $"/p/s/{token}";

    private static CampaignSummaryDto MapToSummary(CampaignRow r) =>
        new(r.Id, r.OrganizationId, r.SurveyId, r.SurveyTitle, r.UnitId, r.UnitName,
            r.DepartmentId, r.DepartmentName, r.Name, r.Audience, r.ResponseGoal,
            r.ActualResponses, CalculateRate(r.ActualResponses, r.ResponseGoal),
            r.StartsAt, r.EndsAt, r.Status, FormatPublicUrl(r.PublicToken),
            r.CreatedAt, r.UpdatedAt);

    private static CampaignDetailDto MapToDetail(CampaignRow r) =>
        new(r.Id, r.OrganizationId, r.SurveyId, r.SurveyTitle, r.UnitId, r.UnitName,
            r.DepartmentId, r.DepartmentName, r.Name, r.Audience, r.ResponseGoal,
            r.ActualResponses, CalculateRate(r.ActualResponses, r.ResponseGoal),
            r.StartsAt, r.EndsAt, r.Status, FormatPublicUrl(r.PublicToken),
            r.CreatedBy, r.CreatedByName, r.CreatedAt, r.UpdatedAt);

    private sealed record CampaignRow {
        public Guid Id { get; init; }
        public Guid OrganizationId { get; init; }
        public Guid SurveyId { get; init; }
        public string SurveyTitle { get; init; } = string.Empty;
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
        public string? PublicToken { get; init; }
        public Guid? CreatedBy { get; init; }
        public string? CreatedByName { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
