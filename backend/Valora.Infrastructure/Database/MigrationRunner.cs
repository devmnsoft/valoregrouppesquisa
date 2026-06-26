using Dapper;
using Microsoft.Extensions.Logging;

namespace Valora.Infrastructure.Database;

public sealed class MigrationRunner(IDbConnectionFactory factory, ILogger<MigrationRunner> logger)
{
    public async Task<IReadOnlyList<string>> RunAsync(string root)
    {
        var directory = Path.Combine(root, "database", "postgresql");
        var files = Directory.GetFiles(directory, "*.sql").OrderBy(Path.GetFileName).ToList();
        using var connection = factory.Create();
        connection.Open();
        await connection.ExecuteAsync("""
            CREATE SCHEMA IF NOT EXISTS valorapesquisa;
            CREATE TABLE IF NOT EXISTS valorapesquisa.schema_migrations (
              script_name text PRIMARY KEY,
              applied_at timestamptz NOT NULL DEFAULT now()
            );
            """);
        var completed = (await connection.QueryAsync<string>(
            "SELECT script_name FROM valorapesquisa.schema_migrations")).ToHashSet();
        var applied = new List<string>();
        foreach (var file in files)
        {
            var scriptName = Path.GetFileName(file);
            if (completed.Contains(scriptName)) continue;
            using var transaction = connection.BeginTransaction();
            try
            {
                await connection.ExecuteAsync(await File.ReadAllTextAsync(file), transaction: transaction);
                await connection.ExecuteAsync(
                    "INSERT INTO valorapesquisa.schema_migrations(script_name) VALUES (@scriptName)",
                    new { scriptName },
                    transaction);
                transaction.Commit();
                applied.Add(scriptName);
                logger.LogInformation("Migration {Migration} aplicada", scriptName);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new InvalidOperationException($"Erro ao aplicar migration {scriptName}: {ex.Message}", ex);
            }
        }
        return applied;
    }
}
