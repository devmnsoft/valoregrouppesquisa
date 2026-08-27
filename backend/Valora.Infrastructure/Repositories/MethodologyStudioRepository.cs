using Dapper;
using Valora.Application.Contracts;
using Valora.Application.Methodology;

namespace Valora.Infrastructure.Repositories;

public sealed class MethodologyStudioRepository(IDbConnectionFactory connections) : IMethodologyStudioRepository
{
    public async Task<IReadOnlyList<MethodologyVersionSummary>> ListVersionsAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT v.id AS "Id",v.code AS "Code",v.name AS "Name",v.status AS "Status",v.version_number AS "VersionNumber",v.is_official AS "IsOfficial",v.published_at AS "PublishedAt",
              (SELECT count(*)::integer FROM valorapesquisa.methodology_concepts c WHERE c.methodology_version_id=v.id AND c.deleted_at IS NULL) AS "Concepts",
              (SELECT count(*)::integer FROM valorapesquisa.methodology_indices i WHERE i.methodology_version_id=v.id AND i.deleted_at IS NULL) AS "Indexes",
              (SELECT count(*)::integer FROM valorapesquisa.methodology_question_bank q WHERE q.methodology_version_id=v.id AND q.deleted_at IS NULL) AS "Questions",
              (SELECT count(*)::integer FROM valorapesquisa.methodology_prompt_templates p WHERE p.methodology_version_id=v.id AND p.deleted_at IS NULL) AS "Prompts"
            FROM valorapesquisa.methodology_versions v WHERE v.deleted_at IS NULL ORDER BY v.is_official DESC,v.version_number DESC
            """;
        using var connection = connections.Create();
        return (await connection.QueryAsync<MethodologyVersionSummary>(new CommandDefinition(sql, cancellationToken: ct))).ToList();
    }

    public async Task<Guid> CreateDraftAsync(string code, string name, string? description, Guid? sourceVersionId, Guid? actorId, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO valorapesquisa.methodology_versions(code,name,description,status,version_number,is_official,metadata_json,created_at,updated_at)
            SELECT @code,@name,@description,'draft',COALESCE((SELECT max(version_number)+1 FROM valorapesquisa.methodology_versions),1),false,
                   jsonb_build_object('clonedFrom',@sourceVersionId,'createdBy',@actorId),now(),now()
            RETURNING id
            """;
        using var connection = connections.Create();
        return await connection.ExecuteScalarAsync<Guid>(new CommandDefinition(sql, new { code, name, description, sourceVersionId, actorId }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<MethodologyValidationIssue>> ValidateAsync(Guid versionId, CancellationToken ct)
    {
        const string sql = "SELECT code \"Code\",severity \"Severity\",entity \"Entity\",message \"Message\" FROM valorapesquisa.validate_methodology_version(@versionId)";
        using var connection = connections.Create();
        return (await connection.QueryAsync<MethodologyValidationIssue>(new CommandDefinition(sql, new { versionId }, cancellationToken: ct))).ToList();
    }

    public async Task PublishAsync(Guid versionId, Guid? actorId, string justification, CancellationToken ct)
    {
        const string sql = "SELECT valorapesquisa.publish_methodology_version(@versionId,@actorId,@justification)";
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { versionId, actorId, justification }, cancellationToken: ct));
    }
}
