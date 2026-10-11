using Npgsql;
using Valora.Tests.Support;

namespace Valora.Tests;

[Trait("Category", "DatabaseContract")]
public sealed class PrivacyRequestDatabaseContractTests {
    [PostgresFact("contrato PostgreSQL de LGPD não executado.")]
    public async Task Public_protocol_column_and_unique_index_match_the_catalog_contract() {
        var connectionString = PostgresTestEnvironment.ConnectionString("contrato PostgreSQL de LGPD não executado.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var columnCommand = new NpgsqlCommand("""
            SELECT data_type, is_nullable
            FROM information_schema.columns
            WHERE table_schema = 'valorapesquisa'
              AND table_name = 'privacy_requests'
              AND column_name = 'protocol'
            """, connection);
        await using var column = await columnCommand.ExecuteReaderAsync();
        Assert.True(await column.ReadAsync(), "valorapesquisa.privacy_requests.protocol was not found.");
        Assert.Equal("text", column.GetString(0));
        Assert.Equal("NO", column.GetString(1));
        await column.CloseAsync();

        await using var indexCommand = new NpgsqlCommand("""
            SELECT i.indisunique, pg_get_indexdef(i.indexrelid)
            FROM pg_index i
            JOIN pg_class idx ON idx.oid = i.indexrelid
            JOIN pg_class tbl ON tbl.oid = i.indrelid
            JOIN pg_namespace n ON n.oid = tbl.relnamespace
            WHERE n.nspname = 'valorapesquisa'
              AND tbl.relname = 'privacy_requests'
              AND idx.relname = 'idx_privacy_requests_protocol'
            """, connection);
        await using var index = await indexCommand.ExecuteReaderAsync();
        Assert.True(await index.ReadAsync(), "idx_privacy_requests_protocol was not found.");
        Assert.True(index.GetBoolean(0));
        Assert.Contains("(protocol)", index.GetString(1), StringComparison.OrdinalIgnoreCase);
    }
}
