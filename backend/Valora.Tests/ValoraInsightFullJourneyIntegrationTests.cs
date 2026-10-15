using System.Data;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Valora.Application.CommercialDelivery;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.DiagnosticWorkspace;
using Valora.Application.Exceptions;
using Valora.Application.OrganizationalIntelligence;
using Valora.Application.ReadModels;
using Valora.Application.Results;
using Valora.Application.Services;
using Valora.Infrastructure.Database;
using Valora.Infrastructure.Repositories;
using Valora.Infrastructure.Security;
using Valora.Tests.Support;
using Xunit;

namespace Valora.Tests;

/// <summary>
/// End-to-end integration and regression suite for the Valora Insight complete journey:
/// Stages 1 through 6.
/// </summary>
public sealed class ValoraInsightFullJourneyIntegrationTests {
    private sealed class TestConnectionFactory(string connectionString) : IDbConnectionFactory {
        public IDbConnection Create() => new NpgsqlConnection(connectionString);
    }

    private sealed class TestTransactionFactory(string connectionString) : IDbTransactionFactory {
        private sealed class TestUnitOfWork(NpgsqlConnection conn, NpgsqlTransaction tx, CancellationToken ct) : IUnitOfWork {
            public IDbConnection Connection => conn;
            public IDbTransaction Transaction => tx;
            public CancellationToken CancellationToken => ct;
            public Task CommitAsync() => tx.CommitAsync(ct);
            public Task RollbackAsync() => tx.RollbackAsync(ct);
            public async ValueTask DisposeAsync() {
                await tx.DisposeAsync();
                await conn.DisposeAsync();
            }
        }

        public async Task<IUnitOfWork> BeginAsync(CancellationToken ct = default) {
            var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            var tx = await conn.BeginTransactionAsync(ct);
            return new TestUnitOfWork(conn, tx, ct);
        }
    }

    private sealed class DummyPlanEntitlementService(bool allowed = true) : IPlanEntitlementService {
        public Task<LimitCheckResult> CheckLimitAsync(Guid organizationId, string limitKey, int requestedAmount = 1) =>
            Task.FromResult(new LimitCheckResult(allowed, limitKey, 100, 0, requestedAmount));
        public Task<PlanEntitlements> ResolveAsync(Guid organizationId) =>
            Task.FromResult(new PlanEntitlements("Enterprise", new Dictionary<string, int>(), new Dictionary<string, string>()));
        public Task<UsageDto> GetUsageAsync(Guid organizationId) =>
            Task.FromResult(new UsageDto(1, 10, 1, new Dictionary<string, int>()));
    }

    private static string Connection() =>
        PostgresTestEnvironment.ConnectionString("regressão PostgreSQL da jornada completa não executada.");

    private static async Task<int> ExecAsync(NpgsqlConnection connection, string sql, params (string Name, object? Value)[] parameters) {
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return await command.ExecuteNonQueryAsync();
    }

    private static async Task CleanupOrganizationAsync(string connectionString, Guid organizationId) {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await ExecAsync(connection, """
            DELETE FROM valorapesquisa.intelligence_processing_jobs WHERE organization_id=@o;
            DELETE FROM valorapesquisa.diagnostic_campaign_messages WHERE organization_id=@o;
            DELETE FROM valorapesquisa.diagnostic_campaign_recipients WHERE organization_id=@o;
            DELETE FROM valorapesquisa.diagnostic_campaigns WHERE organization_id=@o;
            DELETE FROM valorapesquisa.survey_invites WHERE organization_id=@o;
            DELETE FROM valorapesquisa.email_jobs WHERE organization_id=@o;
            DELETE FROM valorapesquisa.communications WHERE organization_id=@o;
            DELETE FROM valorapesquisa.certificates WHERE organization_id=@o;
            DELETE FROM valorapesquisa.dimension_scores WHERE organization_id=@o;
            DELETE FROM valorapesquisa.result_scores WHERE organization_id=@o;
            DELETE FROM valorapesquisa.response_answers WHERE response_id IN (SELECT id FROM valorapesquisa.responses WHERE organization_id=@o);
            DELETE FROM valorapesquisa.responses WHERE organization_id=@o;
            DELETE FROM valorapesquisa.action_items WHERE organization_id=@o;
            DELETE FROM valorapesquisa.action_plans WHERE organization_id=@o;
            DELETE FROM valorapesquisa.evidence_items WHERE organization_id=@o;
            DELETE FROM valorapesquisa.diagnostic_cycles WHERE organization_id=@o;
            DELETE FROM valorapesquisa.notifications WHERE organization_id=@o OR user_id IN (SELECT id FROM valorapesquisa.users WHERE organization_id=@o);
            DELETE FROM valorapesquisa.lgpd_consents WHERE organization_id=@o;
            DELETE FROM valorapesquisa.surveys WHERE organization_id=@o;
            DELETE FROM valorapesquisa.questions WHERE id IN (SELECT q.id FROM valorapesquisa.question_versions q JOIN valorapesquisa.form_section_versions s ON s.id=q.section_id JOIN valorapesquisa.form_versions fv ON fv.id=s.form_version_id WHERE fv.organization_id=@o);
            DELETE FROM valorapesquisa.question_option_versions WHERE question_id IN (SELECT q.id FROM valorapesquisa.question_versions q JOIN valorapesquisa.form_section_versions s ON s.id=q.section_id JOIN valorapesquisa.form_versions fv ON fv.id=s.form_version_id WHERE fv.organization_id=@o);
            DELETE FROM valorapesquisa.question_versions WHERE section_id IN (SELECT s.id FROM valorapesquisa.form_section_versions s JOIN valorapesquisa.form_versions fv ON fv.id=s.form_version_id WHERE fv.organization_id=@o);
            DELETE FROM valorapesquisa.form_section_versions WHERE form_version_id IN (SELECT id FROM valorapesquisa.form_versions WHERE organization_id=@o);
            DELETE FROM valorapesquisa.dimensions WHERE form_version_id IN (SELECT id FROM valorapesquisa.form_versions WHERE organization_id=@o);
            DELETE FROM valorapesquisa.form_versions WHERE organization_id=@o;
            DELETE FROM valorapesquisa.forms WHERE organization_id=@o;
            DELETE FROM valorapesquisa.idempotency_keys WHERE organization_id=@o;
            DELETE FROM valorapesquisa.audit_logs WHERE organization_id=@o;
            DELETE FROM valorapesquisa.user_roles WHERE user_id IN (SELECT id FROM valorapesquisa.users WHERE organization_id=@o);
            DELETE FROM valorapesquisa.platform_governance_events WHERE organization_id=@o;
            DELETE FROM valorapesquisa.journey_events WHERE organization_id=@o;
            DELETE FROM valorapesquisa.users WHERE organization_id=@o;
            DELETE FROM valorapesquisa.organizations WHERE id=@o;
            """, ("o", organizationId));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Stage1_Validator_RejectsMissingLgpdConsent() {
        var validator = new PublicSurveyValidator(null!, null!, new DummyPlanEntitlementService());
        var qId = Guid.NewGuid();
        var questions = new List<QuestionPublicReadModel> {
            new(qId, Guid.NewGuid(), Guid.NewGuid(), "Pergunta", "likert_1_5", true, 5, 1)
        };
        var request = new SubmitSurveyResponseRequest("tok",
            new PublicSurveyParticipantRequest("P", "p@test.local", null, false, null, null, null),
            [new PublicSurveyAnswerRequest(qId, "likert_1_5", ScaleValue: 4)],
            LgpdConsent: false,
            CommunicationConsent: false,
            IdempotencyKey: "k1");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            validator.ValidateForSubmitAsync(Guid.NewGuid(), request, questions, []));
        Assert.Contains("LGPD", ex.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Stage1_Validator_RejectsDuplicateAnswersForSameQuestion() {
        var validator = new PublicSurveyValidator(null!, null!, new DummyPlanEntitlementService());
        var qId = Guid.NewGuid();
        var questions = new List<QuestionPublicReadModel> {
            new(qId, Guid.NewGuid(), Guid.NewGuid(), "Pergunta", "likert_1_5", true, 5, 1)
        };
        var request = new SubmitSurveyResponseRequest("tok",
            new PublicSurveyParticipantRequest("P", "p@test.local", null, false, null, null, null),
            [
                new PublicSurveyAnswerRequest(qId, "likert_1_5", ScaleValue: 4),
                new PublicSurveyAnswerRequest(qId, "likert_1_5", ScaleValue: 5)
            ],
            LgpdConsent: true,
            CommunicationConsent: false,
            IdempotencyKey: "k2");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            validator.ValidateForSubmitAsync(Guid.NewGuid(), request, questions, []));
        Assert.Contains("uma vez", ex.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Stage1_Validator_RejectsUnknownQuestionId() {
        var validator = new PublicSurveyValidator(null!, null!, new DummyPlanEntitlementService());
        var qId = Guid.NewGuid();
        var questions = new List<QuestionPublicReadModel> {
            new(qId, Guid.NewGuid(), Guid.NewGuid(), "Pergunta", "likert_1_5", true, 5, 1)
        };
        var unknownQId = Guid.NewGuid();
        var request = new SubmitSurveyResponseRequest("tok",
            new PublicSurveyParticipantRequest("P", "p@test.local", null, false, null, null, null),
            [new PublicSurveyAnswerRequest(unknownQId, "likert_1_5", ScaleValue: 4)],
            LgpdConsent: true,
            CommunicationConsent: false,
            IdempotencyKey: "k3");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            validator.ValidateForSubmitAsync(Guid.NewGuid(), request, questions, []));
        Assert.Contains("desconhecida", ex.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Stage1_Validator_RejectsMissingRequiredQuestion() {
        var validator = new PublicSurveyValidator(null!, null!, new DummyPlanEntitlementService());
        var qId1 = Guid.NewGuid();
        var qId2 = Guid.NewGuid();
        var questions = new List<QuestionPublicReadModel> {
            new(qId1, Guid.NewGuid(), Guid.NewGuid(), "Pergunta 1", "likert_1_5", true, 5, 1),
            new(qId2, Guid.NewGuid(), Guid.NewGuid(), "Pergunta 2", "likert_1_5", true, 5, 2)
        };
        var request = new SubmitSurveyResponseRequest("tok",
            new PublicSurveyParticipantRequest("P", "p@test.local", null, false, null, null, null),
            [new PublicSurveyAnswerRequest(qId1, "likert_1_5", ScaleValue: 4)],
            LgpdConsent: true,
            CommunicationConsent: false,
            IdempotencyKey: "k4");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            validator.ValidateForSubmitAsync(Guid.NewGuid(), request, questions, []));
        Assert.Contains("obrigatórias", ex.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Stage1_Validator_AllowsOptionalQuestionUnanswered() {
        var validator = new PublicSurveyValidator(null!, null!, new DummyPlanEntitlementService());
        var qId1 = Guid.NewGuid();
        var qId2 = Guid.NewGuid();
        var questions = new List<QuestionPublicReadModel> {
            new(qId1, Guid.NewGuid(), Guid.NewGuid(), "Pergunta Obrigatória", "likert_1_5", true, 5, 1),
            new(qId2, Guid.NewGuid(), Guid.NewGuid(), "Pergunta Opcional", "short_text", false, 0, 2)
        };
        var request = new SubmitSurveyResponseRequest("tok",
            new PublicSurveyParticipantRequest("P", "p@test.local", null, false, null, null, null),
            [new PublicSurveyAnswerRequest(qId1, "likert_1_5", ScaleValue: 4)],
            LgpdConsent: true,
            CommunicationConsent: false,
            IdempotencyKey: "k5");

        var ex = await Record.ExceptionAsync(() =>
            validator.ValidateForSubmitAsync(Guid.NewGuid(), request, questions, []));
        Assert.Null(ex);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Stage1_Validator_RejectsTextExceedingLimit() {
        var validator = new PublicSurveyValidator(null!, null!, new DummyPlanEntitlementService());
        var qId = Guid.NewGuid();
        var questions = new List<QuestionPublicReadModel> {
            new(qId, Guid.NewGuid(), Guid.NewGuid(), "Pergunta Curta", "short_text", true, 0, 1)
        };
        var longText = new string('A', 501);
        var request = new SubmitSurveyResponseRequest("tok",
            new PublicSurveyParticipantRequest("P", "p@test.local", null, false, null, null, null),
            [new PublicSurveyAnswerRequest(qId, "short_text", TextValue: longText)],
            LgpdConsent: true,
            CommunicationConsent: false,
            IdempotencyKey: "k6");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            validator.ValidateForSubmitAsync(Guid.NewGuid(), request, questions, []));
        Assert.Contains("limite", ex.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Stage1_QualitativeQuestions_DoNotProduceArtificialScores() {
        var normalizer = new PublicAnswerNormalizer();
        var scorer = new PublicAnswerScorer(normalizer);
        var qId1 = Guid.NewGuid();
        var qId2 = Guid.NewGuid();
        var dimId = Guid.NewGuid();
        var questions = new List<QuestionPublicReadModel> {
            new(qId1, Guid.NewGuid(), dimId, "Relato Aberto", "short_text", true, 0, 1),
            new(qId2, Guid.NewGuid(), dimId, "Detalhamento Qualitativo", "long_text", true, 0, 2)
        };
        var dimensions = new List<FormDimensionReadModel> {
            new(dimId, "Dimensão Qualitativa", "Descrição", 1, 0)
        };
        var answers = new List<PublicSurveyAnswerRequest> {
            new(qId1, "short_text", TextValue: "Meu relato objetivo"),
            new(qId2, "long_text", TextValue: "Detalhamento das práticas observadas no setor.")
        };

        var scored = scorer.Score(questions, dimensions, [], answers);

        Assert.Equal(2, scored.Count);
        Assert.All(scored, s => Assert.Null(s.Score));
        Assert.All(scored, s => Assert.Null(s.MaxScore));
        Assert.DoesNotContain(scored, s => s.Score.HasValue);
    }

    [PostgresFact("regressão PostgreSQL da jornada completa não executada.")]
    public async Task Stage1_To_Stage3_FullPublicSubmission_Qualitative_And_Quantitative_Integration() {
        var connectionString = Connection();
        var factory = new TestConnectionFactory(connectionString);
        var txFactory = new TestTransactionFactory(connectionString);
        var orgRepo = new OrganizationRepository(factory, NullLogger<OrganizationRepository>.Instance);
        var formRepo = new FormRepository(factory, NullLogger<FormRepository>.Instance);
        var surveyRepo = new SurveyRepository(factory, NullLogger<SurveyRepository>.Instance);
        var respRepo = new ResponseRepository(factory, NullLogger<ResponseRepository>.Instance);
        var certRepo = new CertificateRepository(factory, NullLogger<CertificateRepository>.Instance);
        var resultRepo = new ResultRepository(factory, NullLogger<ResultRepository>.Instance);
        var commRepo = new CommunicationRepository(factory, NullLogger<CommunicationRepository>.Instance);
        var auditRepo = new AuditRepository(factory, NullLogger<AuditRepository>.Instance);
        var lgpdRepo = new LgpdRepository(factory);
        var tokenSec = new ResultTokenService();
        var processingRepo = new IntelligenceProcessingJobRepository(factory);
        var processingService = new IntelligenceProcessingJobService(processingRepo);
        var campaignRepo = new DiagnosticCampaignRepository(factory, txFactory);
        var workspaceRepo = new DiagnosticWorkspaceRepository(factory, txFactory);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var orgId = await orgRepo.CreateAsync($"E2E Valora {suffix}", $"e2e-{suffix}@valora.test.local", $"e2e-{suffix}", "enterprise");
        var userId = Guid.NewGuid();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        try {
            // Setup user
            await ExecAsync(connection, """
                INSERT INTO valorapesquisa.users (id, organization_id, email, name, password_hash, status)
                VALUES (@userId, @orgId, @email, 'Admin E2E', 'hash', 'active');
                """, ("userId", (object?)userId), ("orgId", (object?)orgId), ("email", (object?)$"admin-{suffix}@valora.test.local"));

            // 1. Create qualitative form & version via PostgreSQL
            var formId = Guid.NewGuid();
            var formVersionId = Guid.NewGuid();
            var sectionId = Guid.NewGuid();
            var dimId = Guid.NewGuid();
            var qIdText = Guid.NewGuid();

            await ExecAsync(connection, """
                INSERT INTO valorapesquisa.forms (id, organization_id, code, name, title, slug, form_key, status)
                VALUES (@formId, @orgId, 'form-qual-' || @suffix, 'Formulário Diagnóstico Qualitativo E2E', 'Formulário Diagnóstico Qualitativo E2E', 'form-qual-' || @suffix, 'form-qual-' || @suffix, 'published');

                INSERT INTO valorapesquisa.form_versions (id, form_id, organization_id, version, version_number, status, is_immutable, row_version)
                VALUES (@formVersionId, @formId, @orgId, 1, 1, 'published', true, 1);

                UPDATE valorapesquisa.forms SET latest_published_version_id = @formVersionId WHERE id = @formId;

                INSERT INTO valorapesquisa.dimensions (id, form_version_id, code, name, max_score, display_order, position)
                VALUES (@dimId, @formVersionId, 'cultura', 'Cultura Organizacional', 0, 1, 1);

                INSERT INTO valorapesquisa.form_section_versions (id, organization_id, form_version_id, title, position, display_order)
                VALUES (@sectionId, @orgId, @formVersionId, 'Seção Única', 1, 1);

                INSERT INTO valorapesquisa.question_versions (id, organization_id, section_id, code, type, title, required, dimension_code, position, display_order, settings)
                VALUES (@qIdText, @orgId, @sectionId, 'q-text-1', 'short_text', 'Descreva as oportunidades identificadas na liderança', true, 'cultura', 1, 1, '{}'::jsonb);

                INSERT INTO valorapesquisa.questions (id, code, text, display_order, position, question_type, is_qualitative, is_required)
                VALUES (@qIdText, 'q-text-1', 'Descreva as oportunidades identificadas na liderança', 1, 1, 'short_text', true, true);
                """,
                ("formId", (object?)formId), ("orgId", (object?)orgId), ("formVersionId", (object?)formVersionId),
                ("dimId", (object?)dimId), ("sectionId", (object?)sectionId), ("qIdText", (object?)qIdText),
                ("suffix", (object?)suffix));

            // 2. Create Survey linked to published form
            var surveyId = await surveyRepo.CreateAdminAsync(orgId, formId, formVersionId, "Diagnóstico Qualitativo 2026", "Diagnóstico de cultura", null, null);
            var tokenHash = tokenSec.HashToken("token-qual-e2e");
            await ExecAsync(connection, """
                UPDATE valorapesquisa.surveys
                   SET status='published', token_hash=@tokenHash, public_url='https://valora.test/s/token-qual-e2e'
                 WHERE id=@surveyId;
                """, ("tokenHash", (object?)tokenHash), ("surveyId", (object?)surveyId));

            // 3. Verify public read of published questionnaire
            var validator = new PublicSurveyValidator(surveyRepo, formRepo, new DummyPlanEntitlementService());
            var (readSurvey, readForm, readDims, readQuestions, readOptions) = await validator.ValidateForReadAsync(surveyId, new ValidateSurveyRequest("token-qual-e2e", null));
            Assert.Equal("Diagnóstico Qualitativo 2026", readSurvey.Title);
            Assert.Single(readQuestions);
            Assert.Equal("short_text", readQuestions[0].Type);

            // 4. Submit qualitative response
            var normalizer = new PublicAnswerNormalizer();
            var scorer = new PublicAnswerScorer(normalizer);
            var calculator = new ValoraInsightCalculator();
            var txService = new PublicResponseTransactionService(factory, surveyRepo, respRepo, resultRepo, certRepo, commRepo, auditRepo, lgpdRepo, tokenSec, processingService, NullLogger<PublicResponseTransactionService>.Instance);
            var submitter = new PublicSurveySubmitter(validator, scorer, calculator, txService, NullLogger<PublicSurveySubmitter>.Instance);

            var key = $"idem-qual-{Guid.NewGuid():N}";
            var submitReq = new SubmitSurveyResponseRequest(
                "token-qual-e2e",
                new PublicSurveyParticipantRequest("Participante Qualitativo", "participante@qual.test.local", null, false, null, null, null),
                [new PublicSurveyAnswerRequest(qIdText, "short_text", TextValue: "Observamos forte engajamento nas reuniões quinzenais.")],
                LgpdConsent: true,
                CommunicationConsent: false,
                IdempotencyKey: key);

            var submitResult = await submitter.SubmitAsync(surveyId, submitReq);

            Assert.True(submitResult.Ok);
            Assert.NotEqual(Guid.Empty, submitResult.ResponseId);
            Assert.Null(submitResult.Result); // No artificial score!

            // Verify certificate metadata has 'Qualitativo' label
            await using (var chkCmd = new NpgsqlCommand("SELECT maturity_label FROM valorapesquisa.certificates WHERE response_id=@rid", connection)) {
                chkCmd.Parameters.AddWithValue("rid", submitResult.ResponseId);
                var maturityLabel = (string?)await chkCmd.ExecuteScalarAsync();
                Assert.Equal("Qualitativo", maturityLabel);
            }

            // Verify no result_scores or dimension_scores rows exist for qualitative response
            var scoreRow = await resultRepo.GetByResponseAsync(submitResult.ResponseId);
            Assert.Null(scoreRow);
            var dimRows = await resultRepo.GetDimensionsByResponseIdAsync(submitResult.ResponseId);
            Assert.Empty(dimRows);

            // 5. Test Idempotency Replay of Qualitative response
            var replayResult = await submitter.SubmitAsync(surveyId, submitReq);
            Assert.Equal(submitResult.ResponseId, replayResult.ResponseId);
            Assert.Equal(submitResult.ResultToken, replayResult.ResultToken);
            Assert.Null(replayResult.Result);

            // 6. Test Idempotency Conflict when payload changes
            var conflictingReq = submitReq with {
                Answers = [new PublicSurveyAnswerRequest(qIdText, "short_text", TextValue: "Texto diferente com mesma chave.")]
            };
            var conflictEx = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
                submitter.SubmitAsync(surveyId, conflictingReq));
            Assert.StartsWith("IDEMPOTENCY_CONFLICT:", conflictEx.Message);

            // 7. Test Campaign Creation and Correlation Deduplication
            var campaignReq = new CreateCampaignRequest(
                Name: "Campanha E2E 2026",
                Message: "Olá, participe do diagnóstico pelo link.",
                Recipients: [
                    new CampaignRecipientRequest("p1@qual.test.local", true, true),
                    new CampaignRecipientRequest("p2@qual.test.local", true, true)
                ],
                Audience: "Colaboradores de Operações",
                Channel: "email",
                Subject: "Convidamos para o Diagnóstico",
                StartsAt: DateTimeOffset.UtcNow.AddDays(-1),
                EndsAt: DateTimeOffset.UtcNow.AddDays(10),
                TargetParticipationRate: 85);

            var correlation = $"camp-corr-{Guid.NewGuid():N}";
            var createdCampaign1 = await campaignRepo.CreateAsync(orgId, surveyId, userId, campaignReq, correlation, CancellationToken.None);
            Assert.NotNull(createdCampaign1);
            Assert.Equal("Campanha E2E 2026", createdCampaign1!.Name);

            // Replay with identical correlation returns existing campaign without error
            var createdCampaign2 = await campaignRepo.CreateAsync(orgId, surveyId, userId, campaignReq, correlation, CancellationToken.None);
            Assert.NotNull(createdCampaign2);
            Assert.Equal(createdCampaign1.Id, createdCampaign2!.Id);

            // 8. Verify Diagnostic Workspace Participation Module Counters
            var participationItems = await workspaceRepo.ModuleAsync(orgId, surveyId, surveyId, "participation", CancellationToken.None);
            Assert.Single(participationItems);
            var partItem = participationItems[0];
            Assert.Equal("participation", partItem.Code);
            var json = partItem.Data;
            Assert.True(json.TryGetProperty("respostasRecebidas", out var respRecebidas));
            Assert.Equal(1, respRecebidas.GetInt32());
            Assert.True(json.TryGetProperty("respostasValidas", out var respValidas));
            Assert.Equal(1, respValidas.GetInt32());
            Assert.True(json.TryGetProperty("respostasPontuadas", out var respPontuadas));
            Assert.Equal(0, respPontuadas.GetInt32()); // 0 because pure qualitative is NOT artificially scored!
        }
        finally {
            await CleanupOrganizationAsync(connectionString, orgId);
        }
    }
}
