using Dapper;
using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;
namespace Valora.Infrastructure.Repositories;
public sealed class MigrationRepository(IDbConnectionFactory f, ILogger<MigrationRepository> logger):IMigrationRepository
{
 public async Task<dynamic?> GetStatusAsync(){try{using var c=f.Create(); return await c.QuerySingleOrDefaultAsync("SELECT (SELECT count(*) FROM valorapesquisa.import_batches) AS import_batches,(SELECT count(*) FROM valorapesquisa.compare_reports) AS compare_reports");}catch(Exception ex){logger.LogError(ex,"Erro ao consultar status de migração.");throw;}}
 public async Task SaveCompareReportAsync(string reportJson){try{using var c=f.Create(); await c.ExecuteAsync("INSERT INTO valorapesquisa.compare_reports(report_json) VALUES (CAST(@reportJson AS jsonb))",new{reportJson});}catch(Exception ex){logger.LogError(ex,"Erro ao salvar relatório de comparação de migração.");throw;}}
}
