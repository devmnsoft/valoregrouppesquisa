using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Infrastructure.Database;

namespace Valora.Infrastructure.Repositories;
public sealed class MigrationRepository(IDbConnectionFactory f):IMigrationRepository{ public async Task<dynamic?> GetStatusAsync(){using var c=f.Create(); return await c.QuerySingleOrDefaultAsync("SELECT (SELECT count(*) FROM valorapesquisa.import_batches) AS import_batches,(SELECT count(*) FROM valorapesquisa.compare_reports) AS compare_reports");} public async Task SaveCompareReportAsync(string reportJson){using var c=f.Create(); await c.ExecuteAsync("INSERT INTO valorapesquisa.compare_reports(report_json) VALUES (CAST(@reportJson AS jsonb))",new{reportJson});}}
