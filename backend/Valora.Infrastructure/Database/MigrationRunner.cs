using Dapper;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using Valora.Application.Contracts;

namespace Valora.Infrastructure.Database;

public sealed class MigrationRunner(IDbConnectionFactory factory, ILogger<MigrationRunner> logger)
{
    private const string BootstrapFileName = "script_completo.sql";
    private const string BootstrapVersion = "script_completo_2026_07";

    public async Task<IReadOnlyList<string>> RunAsync(string root)
    {
        var directory = Path.Combine(root, "database", "postgresql");
        logger.LogInformation("Migration scan started. Directory={Directory}", directory);
        var files = Directory.GetFiles(directory, "*.sql")
            .OrderBy(file => string.Equals(Path.GetFileName(file), "script_completo.sql", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(Path.GetFileName)
            .ToList();
        using var connection = factory.Create();
        connection.Open();
        try
        {
            await connection.ExecuteAsync("""
                CREATE SCHEMA IF NOT EXISTS valorapesquisa;
                CREATE TABLE IF NOT EXISTS valorapesquisa.schema_migrations (
                  version text PRIMARY KEY,
                  checksum text NOT NULL,
                  applied_at timestamptz NOT NULL DEFAULT now(),
                  applied_by text NOT NULL DEFAULT current_user,
                  application_version text
                );
                ALTER TABLE valorapesquisa.schema_migrations ADD COLUMN IF NOT EXISTS checksum text;
                ALTER TABLE valorapesquisa.schema_migrations ADD COLUMN IF NOT EXISTS applied_at timestamptz NOT NULL DEFAULT now();
                ALTER TABLE valorapesquisa.schema_migrations ADD COLUMN IF NOT EXISTS applied_by text NOT NULL DEFAULT current_user;
                ALTER TABLE valorapesquisa.schema_migrations ADD COLUMN IF NOT EXISTS application_version text;
                """);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "schema_migrations unavailable.");
            throw;
        }
        var completed = (await connection.QueryAsync<string>("SELECT version FROM valorapesquisa.schema_migrations")).ToHashSet();
        var applied = new List<string>();
        foreach (var file in files)
        {
            var scriptName = Path.GetFileName(file);
            var bootstrapAlreadyApplied = string.Equals(scriptName, BootstrapFileName, StringComparison.OrdinalIgnoreCase)
                && completed.Contains(BootstrapVersion);
            if (completed.Contains(scriptName) || bootstrapAlreadyApplied)
            {
                logger.LogInformation("Migration already applied. ScriptName={ScriptName}", scriptName);
                continue;
            }
            using var transaction = connection.BeginTransaction();
            logger.LogInformation("Migration started. ScriptName={ScriptName}", scriptName);
            try
            {
                var script = await File.ReadAllTextAsync(file);
                var checksum = $"sha256:{Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(script))).ToLowerInvariant()}";
                await connection.ExecuteAsync(script, transaction: transaction);
                await connection.ExecuteAsync(
                    "INSERT INTO valorapesquisa.schema_migrations(version, checksum) VALUES (@scriptName, @checksum) ON CONFLICT(version) DO NOTHING",
                    new { scriptName, checksum },
                    transaction);
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
