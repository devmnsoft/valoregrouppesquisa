using System.Data;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Exceptions;
using Valora.Application.Services;
using Valora.Infrastructure.Database;
using Valora.Infrastructure.Repositories;
using Valora.Infrastructure.Security;
using Xunit;
using Xunit.Sdk;

namespace Valora.Tests;

/// <summary>Contract regressions for persistent idempotency of public survey submissions.</summary>
[Trait("Category", "DatabaseContract")]
public sealed class PublicIdempotencyPostgresTests {
    private sealed class TestConnectionFactory(string connectionString) : IDbConnectionFactory {
        public IDbConnection Create() => new NpgsqlConnection(connectionString);
    }

    private static string Connection() {
        var connectionString = Environment.GetEnvironmentVariable("VALORA_TEST_POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw SkipException.ForSkip("VALORA_TEST_POSTGRES_CONNECTION não configurada; regressão PostgreSQL não executada.");
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        Assert.Matches("(?i)(test|teste|homolog|qa)", builder.Database ?? "");
        return connectionString;
    }

    private static async Task<int> ExecAsync(NpgsqlConnection connection, string sql, params (string Name, object? Value)[] parameters) {
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return await command.ExecuteNonQueryAsync();
    }

    private static async Task CleanupOrganizationAsync(string connectionString, params Guid[] organizationIds) {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        foreach (var organizationId in organizationIds) {
            await ExecAsync(connection, """
                DELETE FROM valorapesquisa.outbox_messages WHERE aggregate_id=@o;
                DELETE FROM valorapesquisa.organization_consents WHERE organization_id=@o;
                DELETE FROM valorapesquisa.onboarding_steps WHERE organization_id=@o;
                DELETE FROM valorapesquisa.onboarding_checklists WHERE organization_id=@o;
                DELETE FROM valorapesquisa.audit_logs WHERE organization_id=@o;
                DELETE FROM valorapesquisa.organization_branding WHERE organization_id=@o;
                DELETE FROM valorapesquisa.organization_settings WHERE organization_id=@o;
                DELETE FROM valorapesquisa.subscriptions WHERE organization_id=@o;
                DELETE FROM valorapesquisa.user_scopes WHERE organization_id=@o;
                DELETE FROM valorapesquisa.user_roles WHERE user_id IN (SELECT id FROM valorapesquisa.users WHERE organization_id=@o);
                DELETE FROM valorapesquisa.users WHERE organization_id=@o;
                DELETE FROM valorapesquisa.addresses WHERE organization_id=@o;
                DELETE FROM valorapesquisa.units WHERE organization_id=@o;
                DELETE FROM valorapesquisa.legal_entities WHERE organization_id=@o;
                DELETE FROM valorapesquisa.idempotency_keys WHERE organization_id=@o;
                DELETE FROM valorapesquisa.roles WHERE organization_id=@o;
                DELETE FROM valorapesquisa.organizations WHERE id=@o;
                """, ("o", organizationId));
        }
    }

    private static SubmitSurveyResponseRequest BuildRequest(string idempotencyKey, decimal scaleValue) => new(
        Token: "public-token-e2e",
        Participant: new PublicSurveyParticipantRequest("Participante EVO", "participante@evo.test.local", null, false, "8.0", null, DateTimeOffset.UtcNow.AddMinutes(-1)),
        Answers: [new PublicSurveyAnswerRequest(Guid.Parse("0f0f0f0f-0f0f-0f0f-0f0f-0f0f0f0f0f0f"), "likert_1_5", scaleValue)],
        LgpdConsent: true,
        CommunicationConsent: false,
        IdempotencyKey: idempotencyKey);

    private static SubmitSurveyResponseResult BuildResult(Guid responseId, string resultToken) => new(
        Ok: true,
        ResponseId: responseId,
        ResultToken: resultToken,
        EmailStatus: "cancelled",
        Certificate: new CertificateMetadataDto(responseId, "VAL-0000000000", "metadata-ready", "Participante EVO", "Valora Group", "Diagnóstico EVO", DateTime.UtcNow),
        Result: new ResultScoreDto(40, 100, 40, "Em desenvolvimento", "Dimensão A", "Dimensão B", "Radar EVO", "Verdade estratégica", "Risco", "Próximo nível"));

    [Fact]
    public async Task Public_response_submission_is_idempotent_per_key_and_rejects_conflicting_payloads() {
        var connectionString = Connection();
        var factory = new TestConnectionFactory(connectionString);
        var orgRepo = new OrganizationRepository(factory, NullLogger<OrganizationRepository>.Instance);
        var responses = new ResponseRepository(factory, NullLogger<ResponseRepository>.Instance);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var org = await orgRepo.CreateAsync($"EVO PublicIdem {suffix}", $"pubidem-{suffix}@evo.test.local", $"pubidem-{suffix}", "free");
        var key = $"pubres-test-{Guid.NewGuid():N}";
        var responseId = Guid.NewGuid();
        var resultToken = Guid.NewGuid().ToString("N");
        try {
            var requestA = BuildRequest(key, 4);
            var requestB = BuildRequest(key, 2);
            var hashA = PublicSubmissionIdempotency.ComputeRequestHash(requestA);
            var hashB = PublicSubmissionIdempotency.ComputeRequestHash(requestB);
            Assert.NotEqual(hashA, hashB);

            // 1. Fresh claim: no prior record, nothing stored yet.
            using (var connection = factory.Create()) {
                connection.Open();
                using var transaction = connection.BeginTransaction();
                var claim = await responses.AcquireSubmissionIdempotencyAsync(key, org, hashA, connection, transaction);
                Assert.True(claim.Claimed);
                Assert.Null(claim.StoredResponseBody);
                transaction.Commit();
            }

            // 2. Outcome sealed in the same transaction as the submission work.
            var result = BuildResult(responseId, resultToken);
            var responseJson = JsonSerializer.Serialize(result, JsonSerializerOptions.Web);
            using (var connection = factory.Create()) {
                connection.Open();
                using var transaction = connection.BeginTransaction();
                await responses.StoreSubmissionOutcomeAsync(key, responseJson, connection, transaction);
                transaction.Commit();
            }

            // 3. Replay: same key + same payload returns the sealed result without a new claim.
            using (var connection = factory.Create()) {
                connection.Open();
                using var transaction = connection.BeginTransaction();
                var replay = await responses.AcquireSubmissionIdempotencyAsync(key, org, hashA, connection, transaction);
                Assert.False(replay.Claimed);
                Assert.NotNull(replay.StoredResponseBody);
                transaction.Commit();
            }

            var stored = await responses.GetStoredSubmissionAsync(key);
            Assert.NotNull(stored);
            Assert.Equal(hashA, stored!.RequestHash);
            var roundTripped = JsonSerializer.Deserialize<SubmitSurveyResponseResult>(stored.ResponseBody!, JsonSerializerOptions.Web);
            Assert.NotNull(roundTripped);
            Assert.Equal(result.ResponseId, roundTripped!.ResponseId);
            Assert.Equal(result.ResultToken, roundTripped.ResultToken);
            Assert.Equal(result.Certificate.CertificateCode, roundTripped.Certificate.CertificateCode);
            Assert.Equal(result.Result.TotalScore, roundTripped.Result.TotalScore);

            // 4. Same key with different payload is rejected as a conflict.
            Exception? conflict = null;
            using (var connection = factory.Create()) {
                connection.Open();
                using var transaction = connection.BeginTransaction();
                try {
                    conflict = await Record.ExceptionAsync(() => responses.AcquireSubmissionIdempotencyAsync(key, org, hashB, connection, transaction));
                }
                finally {
                    try { transaction.Rollback(); } catch { }
                }
            }
            var businessRule = Assert.IsType<BusinessRuleAppException>(conflict);
            Assert.StartsWith("IDEMPOTENCY_CONFLICT:", businessRule.Message);
        }
        finally {
            await using var cleanup = new NpgsqlConnection(connectionString);
            await cleanup.OpenAsync();
            await ExecAsync(cleanup, "DELETE FROM valorapesquisa.idempotency_keys WHERE key=@k", ("k", key));
            await CleanupOrganizationAsync(connectionString, org);
        }
    }
}
