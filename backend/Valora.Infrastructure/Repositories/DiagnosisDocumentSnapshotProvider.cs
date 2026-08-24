using Dapper;
using Valora.Application.Contracts;
using Valora.Application.FormalDeliverables;

namespace Valora.Infrastructure.Repositories;

public sealed class DiagnosisDocumentSnapshotProvider(IDbConnectionFactory connections) : IDiagnosisDocumentSnapshotProvider
{
    public async Task<DiagnosisDocumentSnapshot?> LoadAsync(Guid organizationId, Guid diagnosisId, CancellationToken cancellationToken = default)
    {
        using var connection = connections.Create();
        var header = await connection.QuerySingleOrDefaultAsync<SnapshotHeader>(new CommandDefinition("""
            SELECT o.id AS OrganizationId, o.name AS OrganizationName, s.id AS DiagnosisId, s.name AS DiagnosisName,
                   COALESCE(dc.closed_at, dc.processed_at, max(r.submitted_at)) AS CompletedAt,
                   COALESCE(avg(rs.total_score::numeric / NULLIF(rs.max_score, 0) * 100), 0) AS OverallScore,
                   COALESCE(dc.methodology_version, fv.version::text, '1') AS MethodologyVersion
            FROM valorapesquisa.surveys s
            JOIN valorapesquisa.organizations o ON o.id=s.organization_id
            JOIN valorapesquisa.form_versions fv ON fv.id=s.form_version_id
            LEFT JOIN valorapesquisa.diagnostic_cycles dc ON dc.survey_id=s.id AND dc.organization_id=s.organization_id AND dc.deleted_at IS NULL
            LEFT JOIN valorapesquisa.responses r ON r.survey_id=s.id AND r.organization_id=s.organization_id
            LEFT JOIN valorapesquisa.result_scores rs ON rs.response_id=r.id
            WHERE s.id=@DiagnosisId AND s.organization_id=@OrganizationId AND s.deleted_at IS NULL
            GROUP BY o.id, o.name, s.id, s.name, dc.closed_at, dc.processed_at, dc.methodology_version, fv.version
            """, new { OrganizationId = organizationId, DiagnosisId = diagnosisId }, cancellationToken: cancellationToken));
        if (header is null || header.CompletedAt is null) return null;

        var dimensions = (await connection.QueryAsync<DimensionResult>(new CommandDefinition("""
            SELECT d.name AS Name, avg(ds.score::numeric / NULLIF(ds.max_score, 0) * 100) AS Score,
                   CASE WHEN avg(ds.score::numeric / NULLIF(ds.max_score, 0) * 100) >= 80 THEN 'Consolidado'
                        WHEN avg(ds.score::numeric / NULLIF(ds.max_score, 0) * 100) >= 60 THEN 'Em evolução' ELSE 'Prioritário' END AS Interpretation
            FROM valorapesquisa.responses r JOIN valorapesquisa.result_scores rs ON rs.response_id=r.id
            JOIN valorapesquisa.dimension_scores ds ON ds.result_score_id=rs.id JOIN valorapesquisa.dimensions d ON d.id=ds.dimension_id
            WHERE r.organization_id=@OrganizationId AND r.survey_id=@DiagnosisId GROUP BY d.id,d.name ORDER BY d.name
            """, new { OrganizationId = organizationId, DiagnosisId = diagnosisId }, cancellationToken: cancellationToken))).ToArray();
        var level = header.OverallScore >= 80 ? "Consolidado" : header.OverallScore >= 60 ? "Em evolução" : "Em desenvolvimento";
        return new(header.OrganizationId, header.OrganizationName, header.DiagnosisId, header.DiagnosisName,
            header.CompletedAt.Value, header.OverallScore, level, "Valora Insight", header.MethodologyVersion,
            $"Diagnóstico concluído com score geral de {header.OverallScore:0.0}.",
            "Leitura consolidada baseada exclusivamente nas respostas e scores registrados.", dimensions, [], [], [], [], [], [], [], false);
    }

    private sealed record SnapshotHeader(Guid OrganizationId, string OrganizationName, Guid DiagnosisId,
        string DiagnosisName, DateTimeOffset? CompletedAt, decimal OverallScore, string MethodologyVersion);
}
