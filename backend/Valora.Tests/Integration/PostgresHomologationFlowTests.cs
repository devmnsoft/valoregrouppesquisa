using Xunit;
using Xunit.Sdk;
using Npgsql;

namespace Valora.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class PostgresHomologationFlowTests {
    [Fact]
    public async Task HomologationDatabaseHasDurableResponseProcessingContract() {
        var connection = Environment.GetEnvironmentVariable("VALORA_TEST_POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection))
            throw SkipException.ForSkip("VALORA_TEST_POSTGRES_CONNECTION não configurada; homologação PostgreSQL real bloqueada.");

        await using var database = new NpgsqlConnection(connection);
        await database.OpenAsync();
        await using var command = database.CreateCommand();
        command.CommandText = """
            SELECT
              to_regclass('valorapesquisa.responses') IS NOT NULL
              AND to_regclass('valorapesquisa.intelligence_processing_jobs') IS NOT NULL
              AND EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema='valorapesquisa' AND table_name='responses' AND column_name='is_deleted'
              )
              AND EXISTS (
                SELECT 1 FROM pg_indexes
                WHERE schemaname='valorapesquisa' AND indexname='ux_intelligence_processing_active_key'
              );
            """;
        Assert.True((bool)(await command.ExecuteScalarAsync())!, "O bootstrap aplicado não oferece o contrato durável resposta → processamento.");
    }
}
