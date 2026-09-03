using Dapper;
using Valora.Application.Contracts;
using Valora.Application.Journey;

namespace Valora.Infrastructure.Repositories;

public sealed class JourneyRepository(IDbConnectionFactory connections) : IJourneyRepository
{
    private const string Projection = """
        id AS "Id", event_type AS "EventType", title AS "Title", description AS "Description",
        source_type AS "SourceType", source_id AS "SourceId", impact_level AS "ImpactLevel",
        related_dimension AS "RelatedDimension", related_index_code AS "RelatedIndexCode",
        evidence_summary AS "EvidenceSummary", occurred_at AS "OccurredAt", created_at AS "CreatedAt"
        """;

    public async Task<IReadOnlyList<JourneyEventDto>> Timeline(Guid organizationId, JourneyFilter filter, CancellationToken ct)
    {
        var sql = $"""
            SELECT {Projection}
            FROM valorapesquisa.journey_events
            WHERE organization_id = @OrganizationId
              AND deleted_at IS NULL
              AND (@From::timestamp IS NULL OR occurred_at >= @From::timestamp)
              AND (@To::timestamp IS NULL OR occurred_at < @To::timestamp)
              AND (@SourceType::text IS NULL OR source_type = @SourceType::text)
              AND (@RelatedDimension::text IS NULL OR related_dimension = @RelatedDimension::text)
              AND (@RelatedIndexCode::text IS NULL OR related_index_code = @RelatedIndexCode::text)
              AND (@ImpactLevel::text IS NULL OR impact_level = @ImpactLevel::text)
            ORDER BY occurred_at DESC
            """;
        using var db = connections.Create();
        var rows = await db.QueryAsync<JourneyEventDto>(new CommandDefinition(sql, new
        {
            OrganizationId = organizationId,
            filter.From,
            filter.To,
            filter.SourceType,
            filter.RelatedDimension,
            filter.RelatedIndexCode,
            filter.ImpactLevel
        }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<JourneyEventDto?> Get(Guid organizationId, Guid id, CancellationToken ct)
    {
        var sql = $"SELECT {Projection} FROM valorapesquisa.journey_events WHERE organization_id=@OrganizationId AND id=@Id AND deleted_at IS NULL";
        using var db = connections.Create();
        return await db.QuerySingleOrDefaultAsync<JourneyEventDto>(new CommandDefinition(sql, new { OrganizationId = organizationId, Id = id }, cancellationToken: ct));
    }

    public async Task<Guid> Register(Guid organizationId, Guid userId, RegisterJourneyEventRequest request, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO valorapesquisa.journey_events
                (id,organization_id,diagnostic_id,result_id,governance_cycle_id,event_type,title,description,source_type,source_id,impact_level,related_dimension,related_index_code,evidence_summary,occurred_at,created_by_user_id)
            VALUES
                (@Id,@OrganizationId,@DiagnosticId,@ResultId,@GovernanceCycleId,@EventType,@Title,@Description,@SourceType,@SourceId,@ImpactLevel,@RelatedDimension,@RelatedIndexCode,@EvidenceSummary,coalesce(@OccurredAt,now()),@UserId)
            """;
        var id = Guid.NewGuid();
        using var db = connections.Create();
        await db.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = id,
            OrganizationId = organizationId,
            UserId = userId,
            request.DiagnosticId,
            request.ResultId,
            request.GovernanceCycleId,
            request.EventType,
            request.Title,
            request.Description,
            request.SourceType,
            request.SourceId,
            request.ImpactLevel,
            request.RelatedDimension,
            request.RelatedIndexCode,
            request.EvidenceSummary,
            request.OccurredAt
        }, cancellationToken: ct));
        return id;
    }
}
