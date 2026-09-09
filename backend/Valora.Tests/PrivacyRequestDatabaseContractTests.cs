using Npgsql;

namespace Valora.Tests;

[Trait("Category", "DatabaseContract")]
public sealed class PrivacyRequestDatabaseContractTests {
    [Fact]
    public async Task Public_protocol_column_and_unique_index_match_the_catalog_contract() {
        var connectionString = Environment.GetEnvironmentVariable("VALORA_TEST_POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString)) {
            var sql = File.ReadAllText(Support.RepositoryPaths.CanonicalDatabaseScript);
            Assert.Matches(@"(?is)privacy_requests\s*\([^;]*protocol\s+text\s+NOT NULL", sql);
            Assert.Matches(@"(?is)CREATE UNIQUE INDEX(?: IF NOT EXISTS)? idx_privacy_requests_protocol\s+ON\s+valorapesquisa\.privacy_requests\s*\(protocol\)", sql);
            return;
        }

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
