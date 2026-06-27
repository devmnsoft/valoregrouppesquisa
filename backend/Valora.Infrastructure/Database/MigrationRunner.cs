using Dapper;
using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;

namespace Valora.Infrastructure.Database;

public sealed class MigrationRunner(IDbConnectionFactory factory, ILogger<MigrationRunner> logger)
{
    public async Task<IReadOnlyList<string>> RunAsync(string root)
    {
        var directory = Path.Combine(root, "database", "postgresql");
        logger.LogInformation("Migration scan started. Directory={Directory}", directory);
        var files = Directory.GetFiles(directory, "*.sql").OrderBy(Path.GetFileName).ToList();
        using var connection = factory.Create();
        connection.Open();
        try
        {
            await connection.ExecuteAsync("""
                CREATE SCHEMA IF NOT EXISTS valorapesquisa;
                CREATE TABLE IF NOT EXISTS valorapesquisa.schema_migrations (
                  script_name text PRIMARY KEY,
                  applied_at timestamptz NOT NULL DEFAULT now()
                );
                """);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "schema_migrations unavailable.");
            throw;
        }
        var completed = (await connection.QueryAsync<string>("SELECT script_name FROM valorapesquisa.schema_migrations")).ToHashSet();
        var applied = new List<string>();
        foreach (var file in files)
        {
            var scriptName = Path.GetFileName(file);
            if (completed.Contains(scriptName)) { logger.LogInformation("Migration already applied. ScriptName={ScriptName}", scriptName); continue; }
            using var transaction = connection.BeginTransaction();
            logger.LogInformation("Migration started. ScriptName={ScriptName}", scriptName);
            try
            {
                await connection.ExecuteAsync(await File.ReadAllTextAsync(file), transaction: transaction);
                await connection.ExecuteAsync("INSERT INTO valorapesquisa.schema_migrations(script_name) VALUES (@scriptName)", new { scriptName }, transaction);
                transaction.Commit();
                applied.Add(scriptName);
                logger.LogInformation("Migration applied. ScriptName={ScriptName}", scriptName);
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); logger.LogWarning("Migration rollback executed. ScriptName={ScriptName}", scriptName); }
                catch (Exception rollbackEx) { logger.LogError(rollbackEx, "Migration rollback failed. ScriptName={ScriptName}", scriptName); }
                logger.LogError(ex, "Migration failed. ScriptName={ScriptName}", scriptName);
                throw new InvalidOperationException($"Erro ao aplicar migration {scriptName}.", ex);
            }
        }
        return applied;
    }
}
