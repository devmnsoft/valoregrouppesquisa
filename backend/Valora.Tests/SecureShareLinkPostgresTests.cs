using System.Data;
using Npgsql;
using Valora.Application.Contracts;
using Valora.Application.FormalDeliverables;
using Valora.Infrastructure.FormalDeliverables;

namespace Valora.Tests;

[Trait("Category", "DatabaseContract")]
public sealed class SecureShareLinkPostgresTests {
    private sealed class TestConnectionFactory(string connectionString) : IDbConnectionFactory {
        public IDbConnection Create() => new NpgsqlConnection(connectionString);
    }

    private sealed class NoopAudit : IExportAuditService {
        public Task RecordAsync(Guid organizationId, Guid? userId, string action, string resourceType,
            string resourceId, bool succeeded, string? detail = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private static string? Connection() {
        var connectionString = Environment.GetEnvironmentVariable("VALORA_TEST_POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        Assert.Matches("(?i)(test|teste|homolog|qa)", builder.Database ?? "");
        return connectionString;
    }

    private static async Task<int> ExecAsync(NpgsqlConnection connection, string sql, params (string Name, object? Value)[] parameters) {
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> CountLinksAsync(NpgsqlConnection connection, Guid organizationId, string commandId) {
        await using var command = new NpgsqlCommand("""
            SELECT COUNT(*) FROM valorapesquisa.secure_share_links
            WHERE organization_id=@organization AND command_id=@command AND deleted_at IS NULL
            """, connection);
        command.Parameters.AddWithValue("organization", organizationId);
        command.Parameters.AddWithValue("command", commandId);
        return (long)(await command.ExecuteScalarAsync() ?? 0L);
    }

    [Fact]
    public async Task Concurrent_share_creation_with_same_command_creates_once_and_replays() {
        var connectionString = Connection();
        if (connectionString is null)
            return;
        var organizationId = Guid.NewGuid();
        var deliverableId = Guid.NewGuid();
        var resultId = Guid.NewGuid();
        var commandId = $"share-{Guid.NewGuid():N}";
        var requestHash = new string('a', 64);
        var service = new SecureShareLinkService(new ShareLinkRepository(new TestConnectionFactory(connectionString)), new NoopAudit());

        try {
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            async Task<CreatedShareLink> CreateAsync() {
                await start.Task;
                return await service.CreateForDeliverableAsync(organizationId, deliverableId, resultId, Guid.NewGuid(),
                    TimeSpan.FromHours(2), allowDownload: false, commandId, requestHash);
            }

            var first = CreateAsync();
            var second = CreateAsync();
            start.SetResult();
            var results = await Task.WhenAll(first, second);

            Assert.Single(results, r => r.TokenAvailable);
            Assert.Single(results, r => !r.TokenAvailable);
            Assert.Equal(results[0].Id, results[1].Id);

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            Assert.Equal(1, await CountLinksAsync(connection, organizationId, commandId));
        }
        finally {
            await using var cleanup = new NpgsqlConnection(connectionString);
            await cleanup.OpenAsync();
            await ExecAsync(cleanup, """
                DELETE FROM valorapesquisa.secure_share_link_access_logs
                WHERE share_link_id IN (SELECT id FROM valorapesquisa.secure_share_links WHERE organization_id=@organization);
                DELETE FROM valorapesquisa.secure_share_links WHERE organization_id=@organization;
                """, ("organization", organizationId));
        }
    }
}
