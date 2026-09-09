using Dapper;
using Valora.Application.Contracts;

namespace Valora.Infrastructure.Repositories;

public sealed class ExportDataReader(IDbConnectionFactory connections) : IExportDataReader {
    public async Task<ExportDataSet> ReadAsync(Guid organizationId, string entity, string filterJson, CancellationToken cancellationToken) {
        var sql = entity switch {
            "forms" => "SELECT id,name,description,category,status,created_at,updated_at FROM valorapesquisa.forms WHERE organization_id=@organizationId AND deleted_at IS NULL ORDER BY created_at",
            "surveys" => "SELECT id,form_version_id,name,status,created_at,updated_at FROM valorapesquisa.surveys WHERE organization_id=@organizationId AND deleted_at IS NULL ORDER BY created_at",
            "responses" => "SELECT id,survey_id,form_id,submitted_at,created_at,updated_at FROM valorapesquisa.responses WHERE organization_id=@organizationId ORDER BY created_at",
            "results" => "SELECT r.id,r.response_id,s.total_score,s.max_score,r.created_at,r.updated_at FROM valorapesquisa.results r LEFT JOIN valorapesquisa.result_scores s ON s.id=r.result_score_id WHERE r.organization_id=@organizationId ORDER BY r.created_at",
            "audit" => "SELECT id,user_id,action,entity_type,entity_id,message,correlation_id,severity,module,created_at FROM valorapesquisa.audit_logs WHERE organization_id=@organizationId ORDER BY created_at",
            _ => throw new InvalidOperationException("Entidade de exportação não suportada.")
        };
        using var connection = connections.Create();
        var values = (await connection.QueryAsync(new CommandDefinition(sql, new { organizationId }, cancellationToken: cancellationToken))).ToList();
        var rows = values.Select(value => (IReadOnlyDictionary<string, object?>)(IDictionary<string, object?>)value).ToList();
        var columns = rows.FirstOrDefault()?.Keys.ToList() ?? ColumnsFor(entity);
        return new(columns, rows);
    }

    private static IReadOnlyList<string> ColumnsFor(string entity) => entity switch {
        "forms" => ["id", "name", "description", "category", "status", "created_at", "updated_at"],
        "surveys" => ["id", "form_version_id", "name", "status", "created_at", "updated_at"],
        "responses" => ["id", "survey_id", "form_id", "submitted_at", "created_at", "updated_at"],
        "results" => ["id", "response_id", "total_score", "max_score", "created_at", "updated_at"],
        _ => ["id", "user_id", "action", "entity_type", "entity_id", "message", "correlation_id", "severity", "module", "created_at"]
    };
}
