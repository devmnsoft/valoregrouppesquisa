using System.Data;
using System.Text.Json;
using Dapper;
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
            DELETE FROM valorapesquisa.valora_ai_insight_evidence_links WHERE insight_id IN (SELECT id FROM valorapesquisa.valora_ai_insights WHERE organization_id=@o);
            DELETE FROM valorapesquisa.valora_ai_insights WHERE organization_id=@o;
            DELETE FROM valorapesquisa.valora_ai_evidence_items WHERE organization_id=@o;
            DELETE FROM valorapesquisa.valora_ai_runs WHERE organization_id=@o;
            DELETE FROM valorapesquisa.insights WHERE organization_id=@o;
            DELETE FROM valorapesquisa.evolution_cycles WHERE organization_id=@o;
            DELETE FROM valorapesquisa.intelligence_processing_jobs WHERE organization_id=@o;
            DELETE FROM valorapesquisa.diagnostic_campaign_messages WHERE organization_id=@o;
            DELETE FROM valorapesquisa.diagnostic_campaign_recipients WHERE organization_id=@o;
            DELETE FROM valorapesquisa.diagnostic_campaigns WHERE organization_id=@o;
            DELETE FROM valorapesquisa.survey_invites WHERE organization_id=@o;
            DELETE FROM valorapesquisa.email_jobs WHERE organization_id=@o;
            DELETE FROM valorapesquisa.communications WHERE organization_id=@o;
            DELETE FROM valorapesquisa.certificates WHERE organization_id=@o;
            DELETE FROM valorapesquisa.results WHERE organization_id=@o;
            DELETE FROM valorapesquisa.dimension_scores WHERE organization_id=@o;
            DELETE FROM valorapesquisa.result_scores WHERE organization_id=@o;
            DELETE FROM valorapesquisa.evidence_items WHERE organization_id=@o;
            DELETE FROM valorapesquisa.action_items WHERE organization_id=@o;
            DELETE FROM valorapesquisa.action_plans WHERE organization_id=@o;
            DELETE FROM valorapesquisa.response_answers WHERE response_id IN (SELECT id FROM valorapesquisa.responses WHERE organization_id=@o);
            DELETE FROM valorapesquisa.responses WHERE organization_id=@o;
            DELETE FROM valorapesquisa.diagnostic_cycles WHERE organization_id=@o;
            DELETE FROM valorapesquisa.notification_events WHERE organization_id=@o;
            DELETE FROM valorapesquisa.notification_reads WHERE organization_id=@o;
            DELETE FROM valorapesquisa.notification_recipients WHERE organization_id=@o;
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
            DELETE FROM valorapesquisa.organization_modules WHERE organization_id=@o;
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

    [PostgresFact("regressão PostgreSQL da jornada completa não executada.")]
    public async Task Stage4_To_Stage6_MultiTenant_Integrated_Operational_Journey() {
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

        var suffixA = Guid.NewGuid().ToString("N")[..8];
        var suffixB = Guid.NewGuid().ToString("N")[..8];
        var orgIdA = await orgRepo.CreateAsync($"Cliente Alfa {suffixA}", $"alfa-{suffixA}@valora.test.local", $"alfa-{suffixA}", "enterprise");
        var orgIdB = await orgRepo.CreateAsync($"Cliente Beta {suffixB}", $"beta-{suffixB}@valora.test.local", $"beta-{suffixB}", "professional");

        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        try {
            // Enable organizational_intelligence for OrgA and OrgB
            await ExecAsync(connection, """
                INSERT INTO valorapesquisa.organization_modules (organization_id, module_code, enabled)
                VALUES (@oA, 'organizational_intelligence', true), (@oB, 'organizational_intelligence', true)
                ON CONFLICT (organization_id, module_code) DO UPDATE SET enabled=true;
                """, ("oA", (object?)orgIdA), ("oB", (object?)orgIdB));

            // Setup users
            await ExecAsync(connection, """
                INSERT INTO valorapesquisa.users (id, organization_id, email, name, password_hash, status)
                VALUES (@uA, @oA, @emailA, 'Gestor Alfa', 'hash', 'active'),
                       (@uB, @oB, @emailB, 'Gestor Beta', 'hash', 'active');
                """,
                ("uA", (object?)userA), ("oA", (object?)orgIdA), ("emailA", (object?)$"gestor-{suffixA}@valora.test.local"),
                ("uB", (object?)userB), ("oB", (object?)orgIdB), ("emailB", (object?)$"gestor-{suffixB}@valora.test.local"));

            // 1. PREPARAR DIAGNÓSTICO: Questionário + versão publicada para OrgA
            var formIdA = Guid.NewGuid();
            var formVersionIdA = Guid.NewGuid();
            var sectionIdA = Guid.NewGuid();
            var dimIdA = Guid.NewGuid();
            var qIdA = Guid.NewGuid();

            await ExecAsync(connection, """
                INSERT INTO valorapesquisa.forms (id, organization_id, code, name, title, slug, form_key, status)
                VALUES (@formId, @orgId, 'form-alfa-' || @suffix, 'Formulário Maturidade Alfa', 'Formulário Maturidade Alfa', 'form-alfa-' || @suffix, 'form-alfa-' || @suffix, 'published');

                INSERT INTO valorapesquisa.form_versions (id, form_id, organization_id, version, version_number, status, is_immutable, row_version)
                VALUES (@formVersionId, @formId, @orgId, 1, 1, 'published', true, 1);

                UPDATE valorapesquisa.forms SET latest_published_version_id = @formVersionId WHERE id = @formId;

                INSERT INTO valorapesquisa.dimensions (id, form_version_id, code, name, max_score, display_order, position)
                VALUES (@dimId, @formVersionId, 'lideranca', 'Liderança e Governança', 5, 1, 1);

                INSERT INTO valorapesquisa.form_section_versions (id, organization_id, form_version_id, title, position, display_order)
                VALUES (@sectionId, @orgId, @formVersionId, 'Seção Liderança', 1, 1);

                INSERT INTO valorapesquisa.question_versions (id, organization_id, section_id, code, type, title, required, dimension_code, position, display_order, settings)
                VALUES (@qId, @orgId, @sectionId, 'q-lid-1', 'likert_1_5', 'Os objetivos estratégicos são claramente desdobrados para a equipe', true, 'lideranca', 1, 1, '{}'::jsonb);

                INSERT INTO valorapesquisa.questions (id, code, text, display_order, position, question_type, is_qualitative, is_required)
                VALUES (@qId, 'q-lid-1', 'Os objetivos estratégicos são claramente desdobrados para a equipe', 1, 1, 'likert_1_5', false, true);
                """,
                ("formId", (object?)formIdA), ("orgId", (object?)orgIdA), ("formVersionId", (object?)formVersionIdA),
                ("dimId", (object?)dimIdA), ("sectionId", (object?)sectionIdA), ("qId", (object?)qIdA),
                ("suffix", (object?)suffixA));

            // 2. PUBLICAR: Criar e publicar o diagnóstico para OrgA
            var surveyIdA = await surveyRepo.CreateAdminAsync(orgIdA, formIdA, formVersionIdA, "Diagnóstico Alfa 2026", "Avaliação Operacional de Liderança", null, null);
            var tokenHashA = tokenSec.HashToken("token-alfa-journey");
            await ExecAsync(connection, """
                UPDATE valorapesquisa.surveys
                   SET status='published', token_hash=@tokenHash, public_url='https://valora.test/s/token-alfa-journey',
                       starts_at=now() - interval '1 day', expires_at=now() + interval '30 days',
                       public_slug='Lideranças Operacionais', responsible_user_id=@userA
                 WHERE id=@surveyId;
                """, ("tokenHash", (object?)tokenHashA), ("surveyId", (object?)surveyIdA), ("userA", (object?)userA));

            // 3. CAMPANHA & LINK: Criar campanha na OrgA e validar parâmetros
            var campReq = new CreateCampaignRequest(
                Name: "Campanha Liderança Alfa",
                Message: "Prezados líderes, favor responder o diagnóstico.",
                Recipients: [new CampaignRecipientRequest("lider1@alfa.test.local", true, true)],
                Audience: "Lideranças Operacionais",
                Channel: "email",
                Subject: "Convite para Diagnóstico",
                StartsAt: DateTimeOffset.UtcNow.AddDays(-1),
                EndsAt: DateTimeOffset.UtcNow.AddDays(30),
                TargetParticipationRate: 90);

            var camp = await campaignRepo.CreateAsync(orgIdA, surveyIdA, userA, campReq, $"corr-{suffixA}", CancellationToken.None);
            Assert.NotNull(camp);
            Assert.Equal("Campanha Liderança Alfa", camp!.Name);

            // Conferir metadados consolidados no Workspace do Diagnóstico (Etapa 5 / BLOCO B)
            var cycleA = await workspaceRepo.GetCycleAsync(orgIdA, surveyIdA, CancellationToken.None);
            Assert.NotNull(cycleA);
            Assert.Equal("Formulário Maturidade Alfa", cycleA!.FormName);
            Assert.Equal(1, cycleA.FormVersionNumber);
            Assert.Equal("Lideranças Operacionais", cycleA.TargetAudience);
            Assert.Equal("email", cycleA.DistributionChannel);
            Assert.Equal("Gestor Alfa", cycleA.ResponsibleName);
            Assert.Equal(0, cycleA.ResponseCount);

            // 4. ISOLAMENTO MULTIEMPRESA (BLOCO E / Item 15):
            // OrgB NUNCA deve conseguir acessar o ciclo diagnóstico da OrgA
            var cycleBFromA = await workspaceRepo.GetCycleAsync(orgIdB, surveyIdA, CancellationToken.None);
            Assert.Null(cycleBFromA);

            var partBFromA = await workspaceRepo.ModuleAsync(orgIdB, surveyIdA, surveyIdA, "participation", CancellationToken.None);
            Assert.Single(partBFromA);
            Assert.Equal(0, partBFromA[0].Data.GetProperty("respostasRecebidas").GetInt32());
            Assert.Equal(0, partBFromA[0].Data.GetProperty("respostasValidas").GetInt32());

            var evidenceBFromA = await workspaceRepo.EvidenceAsync(orgIdB, surveyIdA, CancellationToken.None);
            Assert.Empty(evidenceBFromA);

            // 5. RESPONDER PUBLICAMENTE: Submissão de participante anônimo na OrgA
            var validator = new PublicSurveyValidator(surveyRepo, formRepo, new DummyPlanEntitlementService());
            var normalizer = new PublicAnswerNormalizer();
            var scorer = new PublicAnswerScorer(normalizer);
            var calculator = new ValoraInsightCalculator();
            var txService = new PublicResponseTransactionService(factory, surveyRepo, respRepo, resultRepo, certRepo, commRepo, auditRepo, lgpdRepo, tokenSec, processingService, NullLogger<PublicResponseTransactionService>.Instance);
            var submitter = new PublicSurveySubmitter(validator, scorer, calculator, txService, NullLogger<PublicSurveySubmitter>.Instance);

            var submitReq = new SubmitSurveyResponseRequest(
                "token-alfa-journey",
                new PublicSurveyParticipantRequest("Líder Alfa 1", "lider1@alfa.test.local", null, false, null, null, null),
                [new PublicSurveyAnswerRequest(qIdA, "likert_1_5", ScaleValue: 5)],
                LgpdConsent: true,
                CommunicationConsent: false,
                IdempotencyKey: $"sub-key-{suffixA}");

            var subResult = await submitter.SubmitAsync(surveyIdA, submitReq);
            Assert.True(subResult.Ok);
            Assert.NotEqual(Guid.Empty, subResult.ResponseId);
            Assert.NotNull(subResult.Result); // Quantitative survey generates real scores!
            Assert.Equal(5m, subResult.Result!.TotalScore);
            Assert.Equal(4m, subResult.Result!.Percentage);

            // 6. CONFERIR PERSISTÊNCIA E CONTADORES
            var refreshedCycleA = await workspaceRepo.GetCycleAsync(orgIdA, surveyIdA, CancellationToken.None);
            Assert.NotNull(refreshedCycleA);
            Assert.Equal(1, refreshedCycleA!.ResponseCount);

            var partA = await workspaceRepo.ModuleAsync(orgIdA, surveyIdA, surveyIdA, "participation", CancellationToken.None);
            Assert.Single(partA);
            var partJson = partA[0].Data;
            Assert.Equal(1, partJson.GetProperty("respostasRecebidas").GetInt32());
            Assert.Equal(1, partJson.GetProperty("respostasValidas").GetInt32());
            Assert.Equal(1, partJson.GetProperty("respostasPontuadas").GetInt32());

            // 7. PROCESSAR INTELIGÊNCIA: Enfileirar job de recálculo
            var jobId = await workspaceRepo.MarkProcessingAsync(orgIdA, refreshedCycleA, userA, $"corr-proc-{suffixA}", CancellationToken.None);
            Assert.NotEqual(Guid.Empty, jobId);
            var hasActiveJob = await workspaceRepo.HasActiveJobAsync(orgIdA, surveyIdA, CancellationToken.None);
            Assert.True(hasActiveJob);

            // 8. EVIDÊNCIAS E ACHADOS: Inserir e consultar achado analítico explicitamente
            var insightId = Guid.NewGuid();
            await ExecAsync(connection, """
                INSERT INTO valorapesquisa.valora_ai_runs (id, organization_id, diagnosis_id, result_id, prompt_code, prompt_version, provider, model, correlation_id, input_json, status)
                VALUES (@runId, @orgId, @surveyId, @resultId, 'diag-eval', 1, 'openai', 'gpt-4o', 'corr-run', '{}'::jsonb, 'completed');

                INSERT INTO valorapesquisa.valora_ai_insights (
                    id, organization_id, diagnostic_id, result_id, ai_run_id, insight_type,
                    title, summary, evidence_summary, related_dimension, severity, priority,
                    confidence_level, limitation, recommendation, status
                ) VALUES (
                    @id, @orgId, @surveyId, @resultId, @runId, 'operational_opportunity',
                    'Alinhamento Executivo de Metas',
                    'As equipes reportam alto entendimento do desdobramento de metas da liderança.',
                    '100% de convergência na pergunta de desdobramento estratégico.',
                    'Liderança e Governança', 'low', 'high',
                    'high', 'Baseado em 1 amostra inicial de liderança operacional.',
                    'Manter cadência de rituais e documentar plano de ação no Action Center.',
                    'pending_review'
                );
                """,
                ("runId", (object?)Guid.NewGuid()), ("orgId", (object?)orgIdA),
                ("surveyId", (object?)surveyIdA), ("resultId", (object?)subResult.ResponseId),
                ("id", (object?)insightId));

            // Consultar módulo insights via workspaceRepo (BLOCO C / Item 8)
            var insights = await workspaceRepo.ModuleAsync(orgIdA, surveyIdA, surveyIdA, "insights", CancellationToken.None);
            Assert.NotEmpty(insights);
            var ins = insights.First(i => i.Id == insightId);
            Assert.Equal("Alinhamento Executivo de Metas", ins.Data.GetProperty("title").GetString());
            Assert.Equal("Liderança e Governança", ins.Data.GetProperty("dimension").GetString());
            Assert.Equal("sugestao_ia", ins.Data.GetProperty("nature").GetString());
            Assert.Equal("high", ins.Data.GetProperty("confidence").GetString());
            Assert.Equal("pending_review", ins.Data.GetProperty("reviewStatus").GetString());

            // 9. CRIAR E RASTREAR PLANO DE AÇÃO VINCULADO AO DIAGNÓSTICO (BLOCO C / Item 10)
            var planId = Guid.NewGuid();
            await ExecAsync(connection, """
                INSERT INTO valorapesquisa.action_plans (
                    id, organization_id, diagnostic_id, title, summary, status,
                    priority, origin_type, origin_id, owner_user_id
                ) VALUES (
                    @id, @orgId, @surveyId, 'Plano de Continuidade e Desdobramento de Metas',
                    'Plano derivado do achado do Diagnóstico Alfa 2026', 'in_execution',
                    'high', 'diagnostic', @surveyId, @userId
                );

                INSERT INTO valorapesquisa.action_items (
                    id, organization_id, action_plan_id, diagnostic_id, title,
                    status, progress_percent, responsible_user_id
                ) VALUES (
                    @itemId, @orgId, @id, @surveyId, 'Instituir reuniões quinzenais de acompanhamento',
                    'in_progress', 50, @userId
                );
                """,
                ("id", (object?)planId), ("orgId", (object?)orgIdA), ("surveyId", (object?)surveyIdA),
                ("userId", (object?)userA), ("itemId", (object?)Guid.NewGuid()));

            var execution = await workspaceRepo.ExecutionAsync(orgIdA, refreshedCycleA, userA, true, CancellationToken.None);
            Assert.NotNull(execution);
            Assert.Contains(execution.Plans, p => p.Id == planId);
            var linkedPlan = execution.Plans.First(p => p.Id == planId);
            Assert.Equal("Plano de Continuidade e Desdobramento de Metas", linkedPlan.Title);
            Assert.Equal(50, linkedPlan.ProgressPercent);
            Assert.Single(execution.Items);

            // 10. ISOLAMENTO DO PLANO: OrgB não consegue visualizar planos da OrgA
            var cycleBDummy = new DiagnosticCycleDto(Guid.NewGuid(), orgIdB, Guid.NewGuid(), null, "Ciclo Beta", null, 1, "1", "active", null, null, null, null, null, 0, 0, 0, 0, null, "pending", null, DateTime.UtcNow, DateTime.UtcNow);
            var executionB = await workspaceRepo.ExecutionAsync(orgIdB, cycleBDummy, userB, true, CancellationToken.None);
            Assert.Empty(executionB.Plans);
            Assert.Empty(executionB.Items);
        }
        finally {
            await CleanupOrganizationAsync(connectionString, orgIdA);
            await CleanupOrganizationAsync(connectionString, orgIdB);
        }
    }

    [Fact]
    public async Task Sprint1_Validation_TrueResponsible_TrueAudience_IsolatedParticipation_And_TenantSegregation() {
        var connectionString = Environment.GetEnvironmentVariable("VALORA_TEST_POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var orgId1 = Guid.NewGuid();
        var orgId2 = Guid.NewGuid();
        var userAdmin = Guid.NewGuid();
        var userGestor = Guid.NewGuid();
        var formId = Guid.NewGuid();
        var formVersionId = Guid.NewGuid();
        var surveyId1 = Guid.NewGuid();
        var surveyId2 = Guid.NewGuid();
        var cycleId1 = Guid.NewGuid();
        var cycleId2 = Guid.NewGuid();

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        // Garantir colunas adicionadas nesta sprint de forma idempotente
        await ExecAsync(conn, """
            ALTER TABLE valorapesquisa.surveys ADD COLUMN IF NOT EXISTS responsible_user_id uuid REFERENCES valorapesquisa.users(id);
            ALTER TABLE valorapesquisa.surveys ADD COLUMN IF NOT EXISTS target_audience text;
            ALTER TABLE valorapesquisa.diagnostic_cycles ADD COLUMN IF NOT EXISTS responsible_user_id uuid REFERENCES valorapesquisa.users(id);
            ALTER TABLE valorapesquisa.diagnostic_cycles ADD COLUMN IF NOT EXISTS target_audience text;
        """);

        var connFactory = new TestConnectionFactory(connectionString);
        var txFactory = new TestTransactionFactory(connectionString);
        var workspaceRepo = new DiagnosticWorkspaceRepository(connFactory, txFactory);

        try {
            // Setup Org 1 e Org 2
            await ExecAsync(conn, "INSERT INTO valorapesquisa.organizations(id,name,slug,status) VALUES (@id1,'Org 1 Primária','org-1-primaria','active'), (@id2,'Org 2 Secundária','org-2-secundaria','active');",
                ("id1", orgId1), ("id2", orgId2));
            await ExecAsync(conn, "INSERT INTO valorapesquisa.organization_modules(organization_id,module_code,enabled) VALUES (@id1,'organizational_intelligence',true), (@id2,'organizational_intelligence',true);",
                ("id1", orgId1), ("id2", orgId2));

            // Users in Org 1: Admin criado primeiro, Gestor criado depois
            await ExecAsync(conn, """
                INSERT INTO valorapesquisa.users(id,organization_id,name,email,password_hash,status,created_at) VALUES
                (@u1, @id1, 'Admin Primeiro', 'admin1@example.com', 'hash_pw_1', 'active', now() - interval '2 days'),
                (@u2, @id1, 'Gestor Segundo', 'gestor2@example.com', 'hash_pw_2', 'active', now() - interval '1 day');
                """,
                ("u1", userAdmin), ("u2", userGestor), ("id1", orgId1));

            // Form e versão
            await ExecAsync(conn, """
                INSERT INTO valorapesquisa.forms (id, organization_id, code, name, title, slug, form_key, status)
                VALUES (@f, @id1, 'form_alpha', 'Questionário Alpha', 'Questionário Alpha', 'form_alpha', 'form_alpha', 'published');
                INSERT INTO valorapesquisa.form_versions (id, form_id, organization_id, version, version_number, status, is_immutable, row_version)
                VALUES (@fv, @f, @id1, 1, 1, 'published', true, 1);
                """, ("f", formId), ("fv", formVersionId), ("id1", orgId1));

            // DIAGNÓSTICO 1: Sem responsável designado e sem público-alvo (tem apenas public_slug)
            await ExecAsync(conn, """
                INSERT INTO valorapesquisa.surveys(id,organization_id,form_id,form_version_id,name,title,status,public_slug)
                VALUES (@s1, @id1, @f, @fv, 'Diagnóstico Sem Responsável', 'Diagnóstico Sem Responsável', 'draft', 'slug-link-d1');
                INSERT INTO valorapesquisa.diagnostic_cycles(id,organization_id,survey_id,title,status)
                VALUES (@c1, @id1, @s1, 'Diagnóstico Sem Responsável', 'draft');
                """,
                ("s1", surveyId1), ("c1", cycleId1), ("id1", orgId1), ("f", formId), ("fv", formVersionId));

            var cycle1 = await workspaceRepo.GetCycleAsync(orgId1, surveyId1, CancellationToken.None);
            Assert.NotNull(cycle1);
            // REGRA 1.1: Sem inferência de primeiro usuário ativo da organização
            Assert.Null(cycle1.ResponsibleName);
            // REGRA 1.2: Sem cópia de public_slug como público-alvo
            Assert.Null(cycle1.TargetAudience);

            // DIAGNÓSTICO 2: Com responsável explícito (userGestor) e público-alvo explícito
            await ExecAsync(conn, """
                INSERT INTO valorapesquisa.surveys(id,organization_id,form_id,form_version_id,name,title,status,public_slug,responsible_user_id,target_audience)
                VALUES (@s2, @id1, @f, @fv, 'Diagnóstico Com Responsável', 'Diagnóstico Com Responsável', 'active', 'slug-link-d2', @uGestor, 'Lideranças e Coordenadores');
                INSERT INTO valorapesquisa.diagnostic_cycles(id,organization_id,survey_id,title,status,responsible_user_id,target_audience)
                VALUES (@c2, @id1, @s2, 'Diagnóstico Com Responsável', 'active', @uGestor, 'Lideranças e Coordenadores');
                """,
                ("s2", surveyId2), ("c2", cycleId2), ("id1", orgId1), ("f", formId), ("fv", formVersionId), ("uGestor", userGestor));

            var cycle2 = await workspaceRepo.GetCycleAsync(orgId1, surveyId2, CancellationToken.None);
            Assert.NotNull(cycle2);
            Assert.Equal("Gestor Segundo", cycle2.ResponsibleName);
            Assert.Equal("Lideranças e Coordenadores", cycle2.TargetAudience);

            // REGRA 1.1: Preserva histórico quando o responsável deixa de estar ativo
            await ExecAsync(conn, "UPDATE valorapesquisa.users SET status='inactive' WHERE id=@uGestor;", ("uGestor", userGestor));
            var cycle2AfterInactive = await workspaceRepo.GetCycleAsync(orgId1, surveyId2, CancellationToken.None);
            Assert.NotNull(cycle2AfterInactive);
            Assert.Equal("Gestor Segundo", cycle2AfterInactive.ResponsibleName);

            // REGRA 1.4: Participação e E-mails isolados por diagnóstico na mesma organização
            // Diag 1: 1 campanha, 2 destinatários, 1 resposta
            var campId1 = Guid.NewGuid();
            await ExecAsync(conn, "INSERT INTO valorapesquisa.diagnostic_campaigns(id,organization_id,survey_id,name,channel,status) VALUES (@camp, @id1, @s1, 'Campanha 1', 'email', 'active');",
                ("camp", campId1), ("id1", orgId1), ("s1", surveyId1));
            await ExecAsync(conn, """
                INSERT INTO valorapesquisa.diagnostic_campaign_recipients(id,organization_id,campaign_id,recipient_reference,email_hash,recipient_masked,status,idempotency_key)
                VALUES
                (gen_random_uuid(), @id1, @camp, 'user1@example.com', 'h1', 'u***1@example.com', 'sent', 'k1'),
                (gen_random_uuid(), @id1, @camp, 'user2@example.com', 'h2', 'u***2@example.com', 'sent', 'k2');
                """, ("id1", orgId1), ("camp", campId1));
            await ExecAsync(conn, """
                INSERT INTO valorapesquisa.responses(id,organization_id,survey_id,status,created_at)
                VALUES (gen_random_uuid(), @id1, @s1, 'completed', now());
                """, ("id1", orgId1), ("s1", surveyId1));

            // Diag 2: 1 campanha, 3 destinatários, 2 respostas
            var campId2 = Guid.NewGuid();
            await ExecAsync(conn, "INSERT INTO valorapesquisa.diagnostic_campaigns(id,organization_id,survey_id,name,channel,status) VALUES (@camp, @id1, @s2, 'Campanha 2', 'email', 'active');",
                ("camp", campId2), ("id1", orgId1), ("s2", surveyId2));
            await ExecAsync(conn, """
                INSERT INTO valorapesquisa.diagnostic_campaign_recipients(id,organization_id,campaign_id,recipient_reference,email_hash,recipient_masked,status,idempotency_key)
                VALUES
                (gen_random_uuid(), @id1, @camp, 'user3@example.com', 'h3', 'u***3@example.com', 'sent', 'k3'),
                (gen_random_uuid(), @id1, @camp, 'user4@example.com', 'h4', 'u***4@example.com', 'sent', 'k4'),
                (gen_random_uuid(), @id1, @camp, 'user5@example.com', 'h5', 'u***5@example.com', 'sent', 'k5');
                """, ("id1", orgId1), ("camp", campId2));
            await ExecAsync(conn, """
                INSERT INTO valorapesquisa.responses(id,organization_id,survey_id,status,created_at)
                VALUES
                (gen_random_uuid(), @id1, @s2, 'completed', now()),
                (gen_random_uuid(), @id1, @s2, 'completed', now());
                """, ("id1", orgId1), ("s2", surveyId2));

            // Consultar participação do Diagnóstico 1
            var partList1 = await workspaceRepo.ModuleAsync(orgId1, cycleId1, surveyId1, "participation", CancellationToken.None);
            Assert.NotEmpty(partList1);
            var part1 = partList1[0].Data;
            Assert.Equal(1, part1.GetProperty("respostasRecebidas").GetInt32());
            Assert.Equal(2, part1.GetProperty("destinatariosEmail").GetInt32());

            // Consultar participação do Diagnóstico 2
            var partList2 = await workspaceRepo.ModuleAsync(orgId1, cycleId2, surveyId2, "participation", CancellationToken.None);
            Assert.NotEmpty(partList2);
            var part2 = partList2[0].Data;
            Assert.Equal(2, part2.GetProperty("respostasRecebidas").GetInt32());
            Assert.Equal(3, part2.GetProperty("destinatariosEmail").GetInt32());

            // Multi-tenant: Org 2 não consegue consultar ciclo nem participação de Org 1
            var crossCycle = await workspaceRepo.GetCycleAsync(orgId2, surveyId1, CancellationToken.None);
            Assert.Null(crossCycle);
            var crossPartList = await workspaceRepo.ModuleAsync(orgId2, cycleId1, surveyId1, "participation", CancellationToken.None);
            Assert.NotEmpty(crossPartList);
            var crossPart = crossPartList[0].Data;
            Assert.Equal(0, crossPart.GetProperty("respostasRecebidas").GetInt32());
            Assert.Equal(0, crossPart.GetProperty("destinatariosEmail").GetInt32());
        }
        finally {
            await CleanupOrganizationAsync(connectionString, orgId1);
            await CleanupOrganizationAsync(connectionString, orgId2);
        }
    }

    [PostgresFact("regressão PostgreSQL da jornada completa não executada.")]
    public async Task Sprint2_Validation_Preparation_Parameters_Methodology_Lock_And_Concurrency() {
        var connectionString = Connection();
        var connFactory = new TestConnectionFactory(connectionString);
        var txFactory = new TestTransactionFactory(connectionString);
        var workspaceRepo = new DiagnosticWorkspaceRepository(connFactory, txFactory);

        var orgId = Guid.NewGuid();
        var userAdmin = Guid.NewGuid();
        var userGestor1 = Guid.NewGuid();
        var userGestor2 = Guid.NewGuid();
        var userSuspended = Guid.NewGuid();
        var formId = Guid.NewGuid();
        var formVersion1 = Guid.NewGuid();
        var formVersion2 = Guid.NewGuid();
        var sectionId1 = Guid.NewGuid();
        var sectionId2 = Guid.NewGuid();
        var qId1 = Guid.NewGuid();
        var qId2 = Guid.NewGuid();
        var surveyId = Guid.NewGuid();
        var cycleId = Guid.NewGuid();

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        // Ensure row_version columns exist idempotently
        await ExecAsync(conn, """
            ALTER TABLE valorapesquisa.surveys ADD COLUMN IF NOT EXISTS row_version integer NOT NULL DEFAULT 1;
            ALTER TABLE valorapesquisa.diagnostic_cycles ADD COLUMN IF NOT EXISTS row_version integer NOT NULL DEFAULT 1;
            """);

        try {
            await ExecAsync(conn, "INSERT INTO valorapesquisa.organizations(id,name,slug,status) VALUES (@o,'Org Sprint 2','org-sprint-2','active');", ("o", orgId));
            await ExecAsync(conn, "INSERT INTO valorapesquisa.organization_modules(organization_id,module_code,enabled) VALUES (@o,'organizational_intelligence',true);", ("o", orgId));

            // Users: 2 active, 1 suspended
            await ExecAsync(conn, """
                INSERT INTO valorapesquisa.users(id,organization_id,name,email,password_hash,status) VALUES
                (@u1, @o, 'Gestor Titular', 'titular@valora.test', 'hash', 'active'),
                (@u2, @o, 'Gestor Adjunto', 'adjunto@valora.test', 'hash', 'active'),
                (@u3, @o, 'Usuário Suspenso', 'suspenso@valora.test', 'hash', 'suspended');
                """, ("u1", userGestor1), ("u2", userGestor2), ("u3", userSuspended), ("o", orgId));

            // Form with 2 published versions
            await ExecAsync(conn, """
                INSERT INTO valorapesquisa.forms (id, organization_id, code, name, title, slug, form_key, status)
                VALUES (@f, @o, 'form_prep', 'Questionário Preparação', 'Questionário Preparação', 'form_prep', 'form_prep', 'published');

                INSERT INTO valorapesquisa.form_versions (id, form_id, organization_id, version, version_number, status, is_immutable, row_version)
                VALUES
                (@fv1, @f, @o, 1, 1, 'published', true, 1),
                (@fv2, @f, @o, 2, 2, 'published', true, 1);

                INSERT INTO valorapesquisa.form_section_versions (id, organization_id, form_version_id, title, position, display_order)
                VALUES
                (@s1, @o, @fv1, 'Seção 1', 1, 1),
                (@s2, @o, @fv2, 'Seção 2', 1, 1);

                INSERT INTO valorapesquisa.question_versions (id, organization_id, section_id, code, type, title, required, position, display_order, settings)
                VALUES
                (@q1, @o, @s1, 'q1', 'likert_1_5', 'Pergunta V1', true, 1, 1, '{}'::jsonb),
                (@q2, @o, @s2, 'q2', 'likert_1_5', 'Pergunta V2', true, 1, 1, '{}'::jsonb);
                """,
                ("f", formId), ("fv1", formVersion1), ("fv2", formVersion2),
                ("s1", sectionId1), ("s2", sectionId2), ("q1", qId1), ("q2", qId2), ("o", orgId));

            // Initial Survey in draft linked to version 1, 0 responses
            await ExecAsync(conn, """
                INSERT INTO valorapesquisa.surveys(id,organization_id,form_id,form_version_id,name,title,status,row_version)
                VALUES (@s, @o, @f, @fv1, 'Diagnóstico Inicial Rascunho', 'Diagnóstico Inicial Rascunho', 'draft', 1);

                INSERT INTO valorapesquisa.diagnostic_cycles(id,organization_id,survey_id,form_id,title,status,row_version)
                VALUES (@c, @o, @s, @f, 'Diagnóstico Inicial Rascunho', 'draft', 1);
                """, ("s", surveyId), ("c", cycleId), ("f", formId), ("fv1", formVersion1), ("o", orgId));

            // 1. GetPreparationOptionsAsync: Verifica usuários elegíveis e versões disponíveis
            var options = await workspaceRepo.GetPreparationOptionsAsync(orgId, surveyId, CancellationToken.None);
            Assert.NotNull(options);
            Assert.Contains(options.EligibleUsers, u => u.Id == userGestor1 && u.Name == "Gestor Titular");
            Assert.Contains(options.EligibleUsers, u => u.Id == userGestor2 && u.Name == "Gestor Adjunto");
            Assert.DoesNotContain(options.EligibleUsers, u => u.Id == userSuspended); // Suspenso excluído
            Assert.Equal(2, options.AvailableVersions.Count);
            Assert.True(options.CanChangeMethodology, "Com 0 respostas em draft, metodologia deve ser alterável.");
            Assert.Null(options.MethodologyLockReason);

            // 2. Validações de entrada: Não salva dados corrompidos
            // Título inválido (< 3 caracteres)
            await Assert.ThrowsAsync<ArgumentException>(() => workspaceRepo.UpdatePreparationAsync(orgId, surveyId,
                new UpdateDiagnosticPreparationDto("Oi", "Desc", "Público", userGestor1, null, null, null, 1),
                userAdmin, "corr-test", CancellationToken.None));

            // Prazo incoerente (término anterior ao início)
            var start = DateTime.UtcNow.AddDays(5);
            var end = DateTime.UtcNow.AddDays(1);
            await Assert.ThrowsAsync<ArgumentException>(() => workspaceRepo.UpdatePreparationAsync(orgId, surveyId,
                new UpdateDiagnosticPreparationDto("Diagnóstico Válido", "Desc", "Público", userGestor1, start, end, null, 1),
                userAdmin, "corr-test", CancellationToken.None));

            // Usuário suspenso selecionado como responsável
            await Assert.ThrowsAsync<ArgumentException>(() => workspaceRepo.UpdatePreparationAsync(orgId, surveyId,
                new UpdateDiagnosticPreparationDto("Diagnóstico Válido", "Desc", "Público", userSuspended, null, null, null, 1),
                userAdmin, "corr-test", CancellationToken.None));

            // 3. Atualização Válida: Altera título, público-alvo, responsável e versão do formulário (v1 -> v2)
            var validStart = DateTime.UtcNow.AddDays(1);
            var validEnd = DateTime.UtcNow.AddDays(30);
            var updatedCycle = await workspaceRepo.UpdatePreparationAsync(orgId, surveyId,
                new UpdateDiagnosticPreparationDto("Diagnóstico Estratégico 2026", "Avaliação de Maturidade", "Liderança Executiva", userGestor1, validStart, validEnd, formVersion2, 1),
                userAdmin, "corr-test", CancellationToken.None);

            Assert.NotNull(updatedCycle);
            Assert.Equal("Diagnóstico Estratégico 2026", updatedCycle.Title);
            Assert.Equal("Avaliação de Maturidade", updatedCycle.Description);
            Assert.Equal("Liderança Executiva", updatedCycle.TargetAudience);
            Assert.Equal("Gestor Titular", updatedCycle.ResponsibleName);
            Assert.Equal(userGestor1, updatedCycle.ResponsibleUserId);
            Assert.Equal(2, updatedCycle.FormVersionNumber);
            Assert.Equal(2, updatedCycle.RowVersion);

            // Persistência confirmada em nova leitura
            var reloadedCycle = await workspaceRepo.GetCycleAsync(orgId, surveyId, CancellationToken.None);
            Assert.NotNull(reloadedCycle);
            Assert.Equal("Diagnóstico Estratégico 2026", reloadedCycle!.Title);
            Assert.Equal("Liderança Executiva", reloadedCycle.TargetAudience);
            Assert.Equal("Gestor Titular", reloadedCycle.ResponsibleName);
            Assert.Equal(2, reloadedCycle.RowVersion);

            // 4. Proteção da Metodologia com Respostas Coletadas:
            // Inserir 1 resposta
            await ExecAsync(conn, """
                INSERT INTO valorapesquisa.responses(id, organization_id, survey_id, status, submitted_at)
                VALUES (gen_random_uuid(), @o, @s, 'completed', now());
                """, ("o", orgId), ("s", surveyId));

            var optionsWithResponses = await workspaceRepo.GetPreparationOptionsAsync(orgId, surveyId, CancellationToken.None);
            Assert.False(optionsWithResponses.CanChangeMethodology, "Com respostas coletadas, metodologia DEVE ser bloqueada.");
            Assert.NotNull(optionsWithResponses.MethodologyLockReason);
            Assert.Contains("1 respostas coletadas", optionsWithResponses.MethodologyLockReason);

            // Tentar alterar versão do formulário com respostas existentes deve lançar exceção
            await Assert.ThrowsAsync<InvalidOperationException>(() => workspaceRepo.UpdatePreparationAsync(orgId, surveyId,
                new UpdateDiagnosticPreparationDto("Diagnóstico Estratégico 2026", "Desc", "Liderança Executiva", userGestor1, validStart, validEnd, formVersion1, 2),
                userAdmin, "corr-test", CancellationToken.None));

            // 5. Concorrência Otimista (RowVersion):
            // Enviar rowVersion obsoleto (1, enquanto o atual é 2)
            await Assert.ThrowsAsync<DiagnosticConcurrencyException>(() => workspaceRepo.UpdatePreparationAsync(orgId, surveyId,
                new UpdateDiagnosticPreparationDto("Diagnóstico Modificado Paralelo", "Desc", "Liderança Executiva", userGestor1, validStart, validEnd, null, 1),
                userAdmin, "corr-test", CancellationToken.None));

            // Atualização com rowVersion correto (2) funciona e avança para 3
            var cycleV3 = await workspaceRepo.UpdatePreparationAsync(orgId, surveyId,
                new UpdateDiagnosticPreparationDto("Diagnóstico Atualizado V3", "Desc", "Liderança Executiva", userGestor2, validStart, validEnd, null, 2),
                userAdmin, "corr-test", CancellationToken.None);

            Assert.Equal("Diagnóstico Atualizado V3", cycleV3.Title);
            Assert.Equal("Gestor Adjunto", cycleV3.ResponsibleName);
            Assert.Equal(3, cycleV3.RowVersion);
        }
        finally {
            await CleanupOrganizationAsync(connectionString, orgId);
        }
    }

    [PostgresFact("regressão PostgreSQL da jornada completa não executada.")]
    public async Task Sprint3_Validation_Participation_Metrics_Transparency_And_NextAction_State() {
        var connectionString = Connection();
        var connFactory = new TestConnectionFactory(connectionString);
        var txFactory = new TestTransactionFactory(connectionString);
        var workspaceRepo = new DiagnosticWorkspaceRepository(connFactory, txFactory);

        var orgId = Guid.NewGuid();
        var surveyId = Guid.NewGuid();
        var cycleId = Guid.NewGuid();
        var formId = Guid.NewGuid();
        var formVersionId = Guid.NewGuid();

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        try {
            await ExecAsync(conn, "INSERT INTO valorapesquisa.organizations(id,name,slug,status) VALUES (@o,'Org Sprint 3','org-sprint-3','active');", ("o", orgId));
            await ExecAsync(conn, "INSERT INTO valorapesquisa.organization_modules(organization_id,module_code,enabled) VALUES (@o,'organizational_intelligence',true);", ("o", orgId));

            await ExecAsync(conn, """
                INSERT INTO valorapesquisa.forms (id, organization_id, code, name, title, slug, form_key, status)
                VALUES (@f, @o, 'form_sp3', 'Form Sprint 3', 'Form Sprint 3', 'form_sp3', 'form_sp3', 'published');

                INSERT INTO valorapesquisa.form_versions (id, form_id, organization_id, version, version_number, status, is_immutable, row_version)
                VALUES (@fv, @f, @o, 1, 1, 'published', true, 1);

                INSERT INTO valorapesquisa.surveys(id,organization_id,form_id,form_version_id,name,title,status,row_version)
                VALUES (@s, @o, @f, @fv, 'Diagnóstico Sprint 3', 'Diagnóstico Sprint 3', 'active', 1);

                INSERT INTO valorapesquisa.diagnostic_cycles(id,organization_id,survey_id,form_id,title,status,row_version)
                VALUES (@c, @o, @s, @f, 'Diagnóstico Sprint 3', 'active', 1);
                """, ("f", formId), ("fv", formVersionId), ("s", surveyId), ("c", cycleId), ("o", orgId));

            // Insert 3 responses (2 completed, 1 in_progress)
            var resp1 = Guid.NewGuid();
            var resp2 = Guid.NewGuid();
            var resp3 = Guid.NewGuid();
            await ExecAsync(conn, """
                INSERT INTO valorapesquisa.responses(id, organization_id, survey_id, status, submitted_at)
                VALUES
                (@r1, @o, @s, 'completed', now()),
                (@r2, @o, @s, 'completed', now()),
                (@r3, @o, @s, 'in_progress', now());
                """, ("r1", resp1), ("r2", resp2), ("r3", resp3), ("o", orgId), ("s", surveyId));

            // Insert result scores for completed responses
            await ExecAsync(conn, """
                INSERT INTO valorapesquisa.result_scores(id, organization_id, response_id, total_score, max_score)
                VALUES
                (gen_random_uuid(), @o, @r1, 80, 100),
                (gen_random_uuid(), @o, @r2, 85, 100);
                """, ("r1", resp1), ("r2", resp2), ("o", orgId));

            // Query participation module
            var partItems = await workspaceRepo.ModuleAsync(orgId, cycleId, surveyId, "participation", CancellationToken.None);
            Assert.NotEmpty(partItems);
            var partData = partItems[0].Data;

            // 1. Transparency of Counters: Real audited numbers
            Assert.Equal(3, partData.GetProperty("respostasRecebidas").GetInt32());
            Assert.Equal(2, partData.GetProperty("respostasValidas").GetInt32());
            Assert.Equal(2, partData.GetProperty("respostasPontuadas").GetInt32());

            // 2. Audited Sources Metadata
            Assert.True(partData.TryGetProperty("origens", out var origens));
            Assert.False(string.IsNullOrWhiteSpace(origens.GetProperty("respostasRecebidas").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(origens.GetProperty("respostasValidas").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(origens.GetProperty("respostasPontuadas").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(origens.GetProperty("pendenciasProcessamento").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(origens.GetProperty("destinatariosEmail").GetString()));
        }
        finally {
            await CleanupOrganizationAsync(connectionString, orgId);
        }
    }

    [PostgresFact("regressão PostgreSQL da jornada completa não executada.")]
    public async Task Sprint4_Validation_Insights_Canonical_Origins_Editorial_Review_And_ActionCenter_Traceability() {
        var connectionString = Connection();
        var connFactory = new TestConnectionFactory(connectionString);
        var txFactory = new TestTransactionFactory(connectionString);
        var workspaceRepo = new DiagnosticWorkspaceRepository(connFactory, txFactory);
        var actionPlanRepo = new ActionPlanRepository(connFactory, txFactory);

        var orgId = Guid.NewGuid();
        var userAdmin = Guid.NewGuid();
        var surveyId = Guid.NewGuid();
        var cycleId = Guid.NewGuid();
        var formId = Guid.NewGuid();
        var formVersionId = Guid.NewGuid();
        var insightAnalyticalId = Guid.NewGuid();
        var aiInsightApprovedId = Guid.NewGuid();
        var aiInsightPendingId = Guid.NewGuid();
        var actionPlanApprovedId = Guid.NewGuid();
        var actionPlanGeneralId = Guid.NewGuid();

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        try {
            await ExecAsync(conn, "INSERT INTO valorapesquisa.organizations(id,name,slug,status) VALUES (@o,'Org Sprint 4','org-sprint-4','active');", ("o", orgId));
            await ExecAsync(conn, "INSERT INTO valorapesquisa.organization_modules(organization_id,module_code,enabled) VALUES (@o,'organizational_intelligence',true);", ("o", orgId));
            await ExecAsync(conn, "INSERT INTO valorapesquisa.users(id,organization_id,name,email,password_hash,status) VALUES (@u,@o,'Admin Sprint 4','admin.sp4@valora.test','hash','active');", ("u", userAdmin), ("o", orgId));

            await ExecAsync(conn, """
                INSERT INTO valorapesquisa.forms (id, organization_id, code, name, title, slug, form_key, status)
                VALUES (@f, @o, 'form_sp4', 'Form Sprint 4', 'Form Sprint 4', 'form_sp4', 'form_sp4', 'published');

                INSERT INTO valorapesquisa.form_versions (id, form_id, organization_id, version, version_number, status, is_immutable, row_version)
                VALUES (@fv, @f, @o, 1, 1, 'published', true, 1);

                INSERT INTO valorapesquisa.surveys(id,organization_id,form_id,form_version_id,name,title,status,row_version)
                VALUES (@s, @o, @f, @fv, 'Diagnóstico Sprint 4', 'Diagnóstico Sprint 4', 'active', 1);

                INSERT INTO valorapesquisa.diagnostic_cycles(id,organization_id,survey_id,form_id,title,status,row_version)
                VALUES (@c, @o, @s, @f, 'Diagnóstico Sprint 4', 'active', 1);
                """, ("f", formId), ("fv", formVersionId), ("s", surveyId), ("c", cycleId), ("o", orgId));

            // 1. Analytical insight from pipeline (valorapesquisa.insights)
            await ExecAsync(conn, """
                INSERT INTO valorapesquisa.insights(id, organization_id, survey_id, code, status, data)
                VALUES (@id, @o, @s, 'correlacao_lideranca', 'approved', jsonb_build_object(
                    'title', 'Correlação Direta entre Liderança e Eficácia',
                    'dimension', 'Liderança',
                    'calculatedScore', 82.5,
                    'nature', 'fato_calculado'
                ));
                """, ("id", insightAnalyticalId), ("o", orgId), ("s", surveyId));

            // 2. AI insight in editorial queue (valorapesquisa.valora_ai_insights) - approved
            var runId = Guid.NewGuid();
            await ExecAsync(conn, """
                INSERT INTO valorapesquisa.valora_ai_runs (id, organization_id, diagnosis_id, prompt_code, prompt_version, provider, model, correlation_id, input_json, status)
                VALUES (@r, @o, @s, 'eval_prompt', 1, 'openai', 'gpt-4o', 'corr-sp4', '{}'::jsonb, 'completed');
                """, ("r", runId), ("o", orgId), ("s", surveyId));

            await ExecAsync(conn, """
                INSERT INTO valorapesquisa.valora_ai_insights(id, organization_id, diagnostic_id, ai_run_id, insight_type, title, summary, evidence_summary, recommendation, related_dimension, severity, priority, confidence_level, status, review_version)
                VALUES (@id, @o, @s, @r, 'recommendation', 'Plano de Ação para Comunicação', 'Resumo da recomendação', 'Evidência convergente', 'Recomendação estruturada', 'Comunicação', 'medium', 'high', 'Alto', 'converted_to_action', 1);
                """, ("id", aiInsightApprovedId), ("o", orgId), ("s", surveyId), ("r", runId));

            // 3. AI insight in editorial queue - pending review
            await ExecAsync(conn, """
                INSERT INTO valorapesquisa.valora_ai_insights(id, organization_id, diagnostic_id, ai_run_id, insight_type, title, summary, evidence_summary, recommendation, related_dimension, severity, priority, confidence_level, status, review_version)
                VALUES (@id, @o, @s, @r, 'warning', 'Risco de Ruído em Canais Informais', 'Resumo do aviso', 'Evidência textual', 'Recomendação estruturada', 'Comunicação', 'low', 'medium', 'Médio', 'pending_review', 0);
                """, ("id", aiInsightPendingId), ("o", orgId), ("s", surveyId), ("r", runId));

            // 4. Action Plans in Action Center
            // Plan A: Strictly linked to aiInsightApprovedId
            await ExecAsync(conn, """
                INSERT INTO valorapesquisa.action_plans(id, organization_id, title, summary, origin_type, origin_id, diagnostic_id, status, priority, evidence_summary, expected_outcome)
                VALUES (@pid, @o, 'Plano de Ação Aprovado', 'Plano derivado do insight aprovado', 'ai_insight', @origId, @s, 'in_execution', 'high', 'Evidência convergente', 'Melhorar alinhamento');
                """, ("pid", actionPlanApprovedId), ("o", orgId), ("origId", aiInsightApprovedId), ("s", surveyId));

            // Plan B: General plan linked to the survey as a whole (origin_type = 'diagnostic')
            await ExecAsync(conn, """
                INSERT INTO valorapesquisa.action_plans(id, organization_id, title, summary, origin_type, origin_id, diagnostic_id, status, priority, evidence_summary, expected_outcome)
                VALUES (@pid, @o, 'Plano Geral do Diagnóstico', 'Plano macro da pesquisa', 'diagnostic', @s, @s, 'draft', 'medium', 'Metas gerais', 'Concluir ciclo');
                """, ("pid", actionPlanGeneralId), ("o", orgId), ("s", surveyId));

            // Query ModuleAsync("insights")
            var insights = await workspaceRepo.ModuleAsync(orgId, cycleId, surveyId, "insights", CancellationToken.None);
            Assert.NotEmpty(insights);

            // Assert Canonical Discriminators
            var analyticalItem = insights.FirstOrDefault(x => x.Id == insightAnalyticalId);
            Assert.NotNull(analyticalItem);
            Assert.Equal("insight", analyticalItem!.Data.GetProperty("originType").GetString());
            Assert.Equal("fato_calculado", analyticalItem.Data.GetProperty("nature").GetString());

            var aiApprovedItem = insights.FirstOrDefault(x => x.Id == aiInsightApprovedId);
            Assert.NotNull(aiApprovedItem);
            Assert.Equal("valora_ai_insight", aiApprovedItem!.Data.GetProperty("originType").GetString());
            Assert.Equal("decisao_revisada", aiApprovedItem.Data.GetProperty("nature").GetString());
            // Strict link: Points directly to actionPlanApprovedId
            Assert.True(aiApprovedItem.Data.TryGetProperty("linkedPlanId", out var planIdProp));
            Assert.Equal(actionPlanApprovedId, planIdProp.GetGuid());

            var aiPendingItem = insights.FirstOrDefault(x => x.Id == aiInsightPendingId);
            Assert.NotNull(aiPendingItem);
            Assert.Equal("valora_ai_insight", aiPendingItem!.Data.GetProperty("originType").GetString());
            Assert.Equal("sugestao_ia", aiPendingItem.Data.GetProperty("nature").GetString());
            // CRITICAL TEST: Loose fallback (OR ap.diagnostic_id=@surveyId) MUST NOT associate Plan B to this pending insight
            Assert.True(aiPendingItem.Data.TryGetProperty("linkedPlanId", out var pendingPlanProp));
            Assert.True(pendingPlanProp.ValueKind == System.Text.Json.JsonValueKind.Null,
                "Insight pendente NÃO deve ter linkedPlanId vinculado por heurística frágil de survey.");

            // ActionPlanDto: Verify OriginId, DiagnosticId mapping
            var plan = await actionPlanRepo.Get(orgId, userAdmin, actionPlanApprovedId, true, CancellationToken.None);
            Assert.NotNull(plan);
            Assert.Equal(aiInsightApprovedId, plan!.OriginId);
            Assert.Equal(surveyId, plan.DiagnosticId);
            Assert.Equal("ai_insight", plan.OriginType);
        }
        finally {
            await CleanupOrganizationAsync(connectionString, orgId);
        }
    }

    [PostgresFact("regressão PostgreSQL da jornada completa não executada.")]
    public async Task Sprint5_Validation_Homologation_Seed_And_MultiTenant_Gate() {
        var connectionString = Connection();
        var hasher = new Valora.Infrastructure.Security.BCryptPasswordHasher();

        // Path to seed script
        var seedPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "database", "postgresql", "seed_homologacao.sql");
        if (!File.Exists(seedPath)) {
            seedPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "database", "postgresql", "seed_homologacao.sql"));
        }
        Assert.True(File.Exists(seedPath), $"seed_homologacao.sql deve existir no repositório. Procurado em: {seedPath}");

        var seedSql = await File.ReadAllTextAsync(seedPath);

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        // 1. Execute Seed with Secure Dynamic Provisioning (Run twice to prove idempotency)
        var demoHash = hasher.Hash("Admin@123");
        await ExecAsync(conn, "SET valora.allow_demo_seed = 'true';");
        await ExecAsync(conn, $"SET valora.demo_password_hash = '{demoHash}';");
        await ExecAsync(conn, seedSql);
        await ExecAsync(conn, seedSql);

        var orgDemoId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var orgInicioId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var orgIsoId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        // 2. Validate Organizations
        var orgs = (await conn.QueryAsync<(Guid Id, string Name, string Status)>(
            "SELECT id, name, status FROM valorapesquisa.organizations WHERE id IN (@o1, @o2, @o3)",
            new { o1 = orgDemoId, o2 = orgInicioId, o3 = orgIsoId })).ToList();

        Assert.Equal(3, orgs.Count);
        Assert.All(orgs, o => Assert.Equal("active", o.Status));

        // 3. Validate Users, Provisioned Passwords and password_reset_required
        var users = (await conn.QueryAsync<(Guid Id, string Email, string PasswordHash, bool PasswordResetRequired)>(
            "SELECT id, email, password_hash, password_reset_required FROM valorapesquisa.users WHERE organization_id IN (@o1, @o2, @o3)",
            new { o1 = orgDemoId, o2 = orgInicioId, o3 = orgIsoId })).ToList();

        Assert.True(users.Count >= 6, "Seed deve conter no mínimo 6 usuários de homologação.");
        foreach (var user in users) {
            Assert.True(hasher.Verify("Admin@123", user.PasswordHash),
                $"Senha de {user.Email} deve verificar com sucesso contra 'Admin@123'");
            Assert.True(user.PasswordResetRequired,
                $"Usuário {user.Email} deve ter password_reset_required = true no provisionamento de homologação.");
        }

        // 4. Validate Cliente Demonstração has full journey
        var demoSurveys = await conn.ExecuteScalarAsync<int>("SELECT count(*) FROM valorapesquisa.surveys WHERE organization_id=@o", new { o = orgDemoId });
        var demoResponses = await conn.ExecuteScalarAsync<int>("SELECT count(*) FROM valorapesquisa.responses WHERE organization_id=@o", new { o = orgDemoId });
        var demoEvidence = await conn.ExecuteScalarAsync<int>("SELECT count(*) FROM valorapesquisa.evidence_items WHERE organization_id=@o", new { o = orgDemoId });
        var demoInsights = await conn.ExecuteScalarAsync<int>("SELECT count(*) FROM valorapesquisa.valora_ai_insights WHERE organization_id=@o", new { o = orgDemoId });
        var demoPlans = await conn.ExecuteScalarAsync<int>("SELECT count(*) FROM valorapesquisa.action_plans WHERE organization_id=@o", new { o = orgDemoId });

        Assert.True(demoSurveys > 0, "Cliente Demonstração deve conter diagnóstico.");
        Assert.True(demoResponses >= 5, "Cliente Demonstração deve conter respostas.");
        Assert.True(demoEvidence > 0, "Cliente Demonstração deve conter evidências.");
        Assert.True(demoInsights > 0, "Cliente Demonstração deve conter achados.");
        Assert.True(demoPlans > 0, "Cliente Demonstração deve conter planos de ação.");

        // 5. Validate Cliente Início has 0 surveys (fresh onboarding state)
        var inicioSurveys = await conn.ExecuteScalarAsync<int>("SELECT count(*) FROM valorapesquisa.surveys WHERE organization_id=@o", new { o = orgInicioId });
        Assert.Equal(0, inicioSurveys);

        // 6. Validate Cliente Isolamento segregation
        var isoSurveys = await conn.ExecuteScalarAsync<int>("SELECT count(*) FROM valorapesquisa.surveys WHERE organization_id=@o", new { o = orgIsoId });
        Assert.True(isoSurveys > 0);

        var crossTenantResponses = await conn.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM valorapesquisa.responses WHERE organization_id=@iso AND survey_id IN (SELECT id FROM valorapesquisa.surveys WHERE organization_id=@demo)",
            new { iso = orgIsoId, demo = orgDemoId });
        Assert.Equal(0, crossTenantResponses);
    }
}
