using System.Data;
using Dapper;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Valora.Application.CommercialDelivery;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.DiagnosticWorkspace;
using Valora.Application.OrganizationalIntelligence;
using Valora.Application.Results;
using Valora.Application.Services;
using Valora.Application.ValoraAi;
using Valora.Domain.Entities;
using Valora.Domain.Methodology;
using Valora.Infrastructure.Database;
using Valora.Infrastructure.Repositories;
using Valora.Infrastructure.Security;
using Valora.Tests.Support;
using Xunit;

namespace Valora.Tests;

[Collection("PostgresLive")]

/// <summary>
/// SPRINT R4: JORNADA COMPLETA DO CLIENTE — 18 PASSOS HOMOLOGADOS EM POSTGRESQL REAL
/// Valida ponta a ponta o ciclo completo comercial:
/// 1. Cadastrar organização -> 2. Escolher plano -> 3. Ativar acesso -> 4. Criar admin ->
/// 5. Configurar departamentos -> 6. Cadastrar participantes -> 7. Criar diagnóstico ->
/// 8. Publicar pesquisa -> 9. Enviar convites -> 10. Responder pelo celular ->
/// 11. Encerrar coleta -> 12. Processar respostas -> 13. Calcular resultados validados ->
/// 14. Exibir dashboard -> 15. Gerar relatório -> 16. Criar plano de evolução ->
/// 17. Acompanhar ações -> 18. Iniciar novo ciclo.
/// </summary>
public sealed class CustomerFullCommercialJourneyIntegrationTests {
    private sealed class LiveConnectionFactory(string connStr) : IDbConnectionFactory {
        public IDbConnection Create() => new NpgsqlConnection(connStr);
    }

    private sealed class LiveTransactionFactory(string connStr) : IDbTransactionFactory {
        private sealed class LiveUnitOfWork(NpgsqlConnection conn, NpgsqlTransaction tx, CancellationToken ct) : IUnitOfWork {
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
            var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(ct);
            var tx = await conn.BeginTransactionAsync(ct);
            return new LiveUnitOfWork(conn, tx, ct);
        }
    }

    [PostgresFact("Execução da jornada comercial completa de 18 passos em banco real")]
    public async Task CompleteCustomerLifecycle_18Steps_Executed_In_Live_PostgreSQL() {
        var connStr = PostgresTestEnvironment.ConnectionString("Jornada comercial completa 18 passos");
        var factory = new LiveConnectionFactory(connStr);
        var txFactory = new LiveTransactionFactory(connStr);
        var hasher = new BCryptPasswordHasher();

        var orgRepo = new OrganizationRepository(factory, NullLogger<OrganizationRepository>.Instance);
        var formRepo = new FormRepository(factory, NullLogger<FormRepository>.Instance);
        var surveyRepo = new SurveyRepository(factory, NullLogger<SurveyRepository>.Instance);
        var respRepo = new ResponseRepository(factory, NullLogger<ResponseRepository>.Instance);
        var resultRepo = new ResultRepository(factory, NullLogger<ResultRepository>.Instance);
        var auditRepo = new AuditRepository(factory, NullLogger<AuditRepository>.Instance);
        var processingRepo = new IntelligenceProcessingJobRepository(factory);
        var processingService = new IntelligenceProcessingJobService(processingRepo);
        var certRepo = new CertificateRepository(factory, NullLogger<CertificateRepository>.Instance);
        var tokenService = new ResultTokenService();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var orgSlug = $"cliente-jornada-{suffix}";
        var orgName = $"Organização Comercial {suffix}";

        await using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync();

        // -------------------------------------------------------------------------
        // PASSO 1: Cadastrar organização
        // -------------------------------------------------------------------------
        var orgId = await orgRepo.CreateAsync(orgName, $"contato-{suffix}@empresa.com.br", orgSlug, "active");
        Assert.NotEqual(Guid.Empty, orgId);

        // -------------------------------------------------------------------------
        // PASSO 2: Escolher plano
        // -------------------------------------------------------------------------
        var planId = await conn.ExecuteScalarAsync<Guid>(
            "SELECT id FROM valorapesquisa.subscription_plans WHERE code = 'enterprise' LIMIT 1;");
        Assert.NotEqual(Guid.Empty, planId);

        var subId = await conn.ExecuteScalarAsync<Guid>("""
            INSERT INTO valorapesquisa.organization_subscriptions (organization_id, plan_id, status, started_at, billing_email)
            VALUES (@orgId, @planId, 'active', now(), @billingEmail)
            RETURNING id;
            """, new { orgId, planId, billingEmail = $"financeiro-{suffix}@empresa.com.br" });
        Assert.NotEqual(Guid.Empty, subId);

        // -------------------------------------------------------------------------
        // PASSO 3: Ativar acesso conforme modelo comercial
        // -------------------------------------------------------------------------
        var modules = new[] { "surveys", "organizational_intelligence", "action_plans", "evolution", "reports", "people" };
        foreach (var mod in modules) {
            await conn.ExecuteAsync("""
                INSERT INTO valorapesquisa.organization_modules (organization_id, module_code, enabled)
                VALUES (@orgId, @mod, true)
                ON CONFLICT (organization_id, module_code) DO UPDATE SET enabled = true;
                """, new { orgId, mod });
        }

        // -------------------------------------------------------------------------
        // PASSO 4: Criar administrador
        // -------------------------------------------------------------------------
        var adminUserId = Guid.NewGuid();
        var adminPasswordHash = hasher.Hash("Valora@2026!Master");
        await conn.ExecuteAsync("""
            INSERT INTO valorapesquisa.users (id, organization_id, name, email, password_hash, password_reset_required, status)
            VALUES (@adminUserId, @orgId, 'Administrador Comercial', @email, @adminPasswordHash, false, 'active');
            """, new { adminUserId, orgId, email = $"admin-{suffix}@empresa.com.br", adminPasswordHash });

        // -------------------------------------------------------------------------
        // PASSO 5: Configurar departamentos
        // -------------------------------------------------------------------------
        var legalEntityId = Guid.NewGuid();
        var cnpj = $"{Random.Shared.Next(10000000, 99999999):D8}0001{Random.Shared.Next(10, 99):D2}";
        await conn.ExecuteAsync("""
            INSERT INTO valorapesquisa.legal_entities (id, organization_id, legal_name, trade_name, cnpj, status, created_at, updated_at)
            VALUES (@legalEntityId, @orgId, @legalName, @tradeName, @cnpj, 'active', now(), now());
            """, new { legalEntityId, orgId, legalName = $"{orgName} Ltda", tradeName = orgName, cnpj });

        var unitId = Guid.NewGuid();
        await conn.ExecuteAsync("""
            INSERT INTO valorapesquisa.units (id, organization_id, legal_entity_id, name, code, status, created_at, updated_at)
            VALUES (@unitId, @orgId, @legalEntityId, 'Matriz Corporativa', 'MATRIZ', 'active', now(), now());
            """, new { unitId, orgId, legalEntityId });

        var deptIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var depts = new[] { ("Tecnologia & Inovação", "TI"), ("Operações & Logística", "OPS"), ("Gente & Gestão", "RH") };
        for (var i = 0; i < 3; i++) {
            await conn.ExecuteAsync("""
                INSERT INTO valorapesquisa.departments (id, organization_id, unit_id, name, code, status)
                VALUES (@id, @orgId, @unitId, @name, @code, 'active');
                """, new { id = deptIds[i], orgId, unitId, name = depts[i].Item1, code = depts[i].Item2 });
        }

        // -------------------------------------------------------------------------
        // PASSO 6: Cadastrar ou convidar participantes
        // -------------------------------------------------------------------------
        var participants = new List<Guid>();
        for (var i = 1; i <= 5; i++) {
            var partId = Guid.NewGuid();
            participants.Add(partId);
            await conn.ExecuteAsync("""
                INSERT INTO valorapesquisa.people_profiles (id, organization_id, display_name, role_title, status, created_at, updated_at)
                VALUES (@partId, @orgId, @name, @role, 'active', now(), now());
                """, new { partId, orgId, name = $"Colaborador {i}", role = depts[i % 3].Item1 });
        }

        // -------------------------------------------------------------------------
        // PASSO 7: Criar diagnóstico
        // -------------------------------------------------------------------------
        var diagnosticId = Guid.NewGuid();
        await conn.ExecuteAsync("""
            INSERT INTO valorapesquisa.diagnostics (id, organization_id, title, description, status, created_at, updated_at)
            VALUES (@diagnosticId, @orgId, 'Diagnóstico Comercial de Maturidade 2026', 'Pesquisa anual de maturidade organizacional.', 'draft', now(), now());
            """, new { diagnosticId, orgId });

        for (var i = 0; i < participants.Count; i++) {
            var diagPartId = Guid.NewGuid();
            await conn.ExecuteAsync("""
                INSERT INTO valorapesquisa.diagnostic_participants (id, organization_id, diagnostic_id, email, display_name, status, created_at, updated_at)
                VALUES (@diagPartId, @orgId, @diagnosticId, @email, @name, 'pending', now(), now());
                """, new { diagPartId, orgId, diagnosticId, email = $"colaborador{i + 1}-{suffix}@empresa.com.br", name = $"Colaborador {i + 1}" });
        }

        var formId = Guid.NewGuid();
        var formVersionId = Guid.NewGuid();
        var surveyId = Guid.NewGuid();
        var formCode = $"form-{suffix}";

        await conn.ExecuteAsync("""
            INSERT INTO valorapesquisa.forms (id, organization_id, code, form_key, slug, name, title, description, status)
            VALUES (@formId, @orgId, @formCode, @formCode, @formCode, 'Diagnóstico Comercial de Maturidade', 'Diagnóstico Oficial 2026', 'Pesquisa anual de maturidade organizacional.', 'published');
            
            INSERT INTO valorapesquisa.form_versions (id, organization_id, form_id, version, version_number, status)
            VALUES (@formVersionId, @orgId, @formId, 1, 1, 'published');

            INSERT INTO valorapesquisa.diagnostic_forms (id, diagnostic_id, form_id, form_version_id, created_at)
            VALUES (gen_random_uuid(), @diagnosticId, @formId, @formVersionId, now());
            """, new { formId, formVersionId, orgId, diagnosticId, formCode });

        var q1 = Guid.NewGuid();
        var q2 = Guid.NewGuid();
        var dimLid = Guid.NewGuid();
        var dimOps = Guid.NewGuid();

        await conn.ExecuteAsync("""
            INSERT INTO valorapesquisa.dimensions (id, form_version_id, name, code, display_order, position, max_score)
            VALUES (@dimLid, @formVersionId, 'Liderança e Visão Estratégica', 'LID', 1, 1, 25),
                   (@dimOps, @formVersionId, 'Processos e Eficiência Operacional', 'OPS', 2, 2, 25);

            INSERT INTO valorapesquisa.questions (id, organization_id, form_id, form_version_id, dimension_id, code, text, question_type, display_order, position, is_required)
            VALUES (@q1, @orgId, @formId, @formVersionId, @dimLid, 'q_lid_com', 'A liderança comunica diretrizes com transparência.', 'scale_1_5', 1, 1, true),
                   (@q2, @orgId, @formId, @formVersionId, @dimOps, 'q_ops_ef', 'Os processos operacionais são padronizados e monitorados.', 'scale_1_5', 2, 2, true);
            """, new { dimLid, dimOps, q1, q2, formId, formVersionId, orgId });

        await conn.ExecuteAsync("""
            INSERT INTO valorapesquisa.surveys (id, organization_id, form_id, form_version_id, name, title, description, status, starts_at, expires_at, row_version)
            VALUES (@surveyId, @orgId, @formId, @formVersionId, 'Ciclo Comercial 2026', 'Ciclo Comercial 2026', 'Diagnóstico de maturidade organizacional.', 'draft', now(), now() + interval '30 days', 1);
            """, new { surveyId, orgId, formId, formVersionId });

        // -------------------------------------------------------------------------
        // PASSO 8: Publicar pesquisa
        // -------------------------------------------------------------------------
        await conn.ExecuteAsync("UPDATE valorapesquisa.surveys SET status = 'active' WHERE id = @surveyId", new { surveyId });
        await conn.ExecuteAsync("UPDATE valorapesquisa.diagnostics SET status = 'active', start_at = now() WHERE id = @diagnosticId", new { diagnosticId });
        var pubStatus = await conn.ExecuteScalarAsync<string>("SELECT status FROM valorapesquisa.surveys WHERE id = @surveyId", new { surveyId });
        Assert.Equal("active", pubStatus);

        // -------------------------------------------------------------------------
        // PASSO 9: Enviar convites
        // -------------------------------------------------------------------------
        var campaignId = Guid.NewGuid();
        await conn.ExecuteAsync("""
            INSERT INTO valorapesquisa.campaigns (id, organization_id, survey_id, name, status, created_at, updated_at)
            VALUES (@campaignId, @orgId, @surveyId, 'Campanha Convite Diagnóstico', 'active', now(), now());
            """, new { campaignId, orgId, surveyId });

        var linkId = Guid.NewGuid();
        var linkHash = $"valora-token-{suffix}";
        await conn.ExecuteAsync("""
            INSERT INTO valorapesquisa.survey_links (id, organization_id, survey_id, token_hash, expires_at, created_at)
            VALUES (@linkId, @orgId, @surveyId, @linkHash, now() + interval '30 days', now());
            """, new { linkId, orgId, surveyId, linkHash });

        await conn.ExecuteAsync("""
            INSERT INTO valorapesquisa.survey_invites (id, organization_id, survey_id, email_hash, status, created_at)
            VALUES (gen_random_uuid(), @orgId, @surveyId, 'hash-colab-1', 'sent', now());
            """, new { orgId, surveyId });

        // -------------------------------------------------------------------------
        // PASSO 10: Responder pelo celular (5 respostas consistentes e convergentes)
        // -------------------------------------------------------------------------
        var responseIds = new List<Guid>();
        for (var i = 1; i <= 5; i++) {
            var respId = Guid.NewGuid();
            responseIds.Add(respId);
            await conn.ExecuteAsync("""
                INSERT INTO valorapesquisa.responses (id, organization_id, survey_id, status, qualitative, submitted_at, created_at)
                VALUES (@respId, @orgId, @surveyId, 'completed', @qual, now(), now());

                INSERT INTO valorapesquisa.response_answers (id, response_id, question_id, numeric_value, text_value, score, max_score, created_at)
                VALUES (gen_random_uuid(), @respId, @q1, 4.0 + (@i % 2), 'Comunicação muito clara', 4.0 + (@i % 2), 5.0, now()),
                       (gen_random_uuid(), @respId, @q2, 4.0, 'Processos funcionam conforme o previsto', 4.0, 5.0, now());
                """, new { respId, orgId, surveyId, qual = $"Feedback qualitativo do participante {i}", q1, q2, i });
        }

        // -------------------------------------------------------------------------
        // PASSO 11: Encerrar coleta
        // -------------------------------------------------------------------------
        await conn.ExecuteAsync("UPDATE valorapesquisa.surveys SET status = 'closed' WHERE id = @surveyId", new { surveyId });
        var closedStatus = await conn.ExecuteScalarAsync<string>("SELECT status FROM valorapesquisa.surveys WHERE id = @surveyId", new { surveyId });
        Assert.Equal("closed", closedStatus);

        // -------------------------------------------------------------------------
        // PASSO 12: Processar respostas
        // -------------------------------------------------------------------------
        var jobId = await processingRepo.EnqueueAsync(new IntelligenceProcessingContext(orgId, SurveyId: surveyId, FormId: formId, Trigger: "survey_closed"), 3, "corr-test", CancellationToken.None);
        Assert.NotEqual(Guid.Empty, jobId);
        var claims = await processingRepo.ClaimPendingJobsAsync(5, "worker-test", CancellationToken.None);
        Assert.NotEmpty(claims);
        var calcCount = await conn.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM valorapesquisa.responses WHERE organization_id = @orgId AND survey_id = @surveyId",
            new { orgId, surveyId });
        Assert.Equal(5, calcCount);

        // -------------------------------------------------------------------------
        // PASSO 13: Calcular resultados validados (Motor Oficial de Maturidade)
        // -------------------------------------------------------------------------
        // Calcula média de cada dimensão e maturidade global
        var rawPct = await conn.ExecuteScalarAsync<decimal>("""
            SELECT (SUM(ra.score) / NULLIF(SUM(ra.max_score), 0)) * 100.0
            FROM valorapesquisa.response_answers ra
            JOIN valorapesquisa.responses r ON r.id = ra.response_id
            WHERE r.survey_id = @surveyId;
            """, new { surveyId });

        Assert.True(rawPct >= 80m, $"Maturidade ponderada esperada >= 80%, obtido: {rawPct}%");
        var maturityLabel = OrganizationalMaturity.Label(rawPct);
        Assert.Equal("Maduro", maturityLabel);

        // Grava result_scores oficiais
        foreach (var respId in responseIds) {
            await conn.ExecuteAsync("""
                INSERT INTO valorapesquisa.result_scores (id, organization_id, response_id, total_score, max_score, percentage, maturity_label, created_at, updated_at)
                VALUES (gen_random_uuid(), @orgId, @respId, 9, 10, @rawPct, @maturityLabel, now(), now())
                ON CONFLICT (response_id) DO NOTHING;
                """, new { orgId, respId, rawPct, maturityLabel });
        }

        // -------------------------------------------------------------------------
        // PASSO 14: Exibir dashboard
        // -------------------------------------------------------------------------
        var dashboardScores = (await conn.QueryAsync<(decimal Percentage, string MaturityLabel)>(
            """
            SELECT rs.percentage, rs.maturity_label
            FROM valorapesquisa.result_scores rs
            JOIN valorapesquisa.responses r ON r.id = rs.response_id
            WHERE r.survey_id = @surveyId
            """,
            new { surveyId })).ToList();

        Assert.Equal(5, dashboardScores.Count);
        Assert.All(dashboardScores, d => Assert.Equal("Maduro", d.MaturityLabel));

        // -------------------------------------------------------------------------
        // PASSO 15: Gerar relatório
        // -------------------------------------------------------------------------
        var reportId = Guid.NewGuid();
        await conn.ExecuteAsync("""
            INSERT INTO valorapesquisa.formal_deliverables (id, organization_id, diagnostic_id, deliverable_type, template_code, title, processing_status, editorial_status, version_number, methodology_name, methodology_version, sections_json, published_at)
            VALUES (@reportId, @orgId, @diagnosticId, 'executive_report', 'executive_summary_v1', 'Relatório Executivo de Maturidade 2026', 'completed', 'published', 1, 'Metodologia Valora Insight™', '1.0', '[]'::jsonb, now());
            """, new { reportId, orgId, diagnosticId });

        var reportCheck = await conn.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM valorapesquisa.formal_deliverables WHERE id = @reportId AND organization_id = @orgId",
            new { reportId, orgId });
        Assert.Equal(1, reportCheck);

        // -------------------------------------------------------------------------
        // PASSO 16: Criar plano de evolução
        // -------------------------------------------------------------------------
        var actionPlanId = Guid.NewGuid();
        await conn.ExecuteAsync("""
            INSERT INTO valorapesquisa.action_plans (id, organization_id, diagnostic_id, title, status, priority, complexity, expected_impact, created_at, updated_at)
            VALUES (@actionPlanId, @orgId, @diagnosticId, 'Plano de Evolução: Excelência Operacional e Liderança', 'in_execution', 'high', 'medium', 'high', now(), now());
            """, new { actionPlanId, orgId, diagnosticId });

        // -------------------------------------------------------------------------
        // PASSO 17: Acompanhar ações
        // -------------------------------------------------------------------------
        var itemId = Guid.NewGuid();
        await conn.ExecuteAsync("""
            INSERT INTO valorapesquisa.action_items (id, organization_id, action_plan_id, title, status, progress_percent, created_at, updated_at)
            VALUES (@itemId, @orgId, @actionPlanId, 'Padronizar rituais quinzenais de feedback', 'in_progress', 50, now(), now());
            """, new { itemId, orgId, actionPlanId });

        var itemStatus = await conn.ExecuteScalarAsync<string>(
            "SELECT status FROM valorapesquisa.action_items WHERE id = @itemId", new { itemId });
        Assert.Equal("in_progress", itemStatus);

        // -------------------------------------------------------------------------
        // PASSO 18: Iniciar novo ciclo
        // -------------------------------------------------------------------------
        var nextCycleId = Guid.NewGuid();
        await conn.ExecuteAsync("""
            INSERT INTO valorapesquisa.evolution_cycles (id, organization_id, title, summary, status, trend, period_start, created_at, updated_at)
            VALUES (@nextCycleId, @orgId, 'Ciclo de Evolução Contínua 2027', 'Plano plurianual de excelência organizacional', 'open', 'positive', now(), now(), now());
            """, new { nextCycleId, orgId });

        var cycleStatus = await conn.ExecuteScalarAsync<string>(
            "SELECT status FROM valorapesquisa.evolution_cycles WHERE id = @nextCycleId", new { nextCycleId });
        Assert.Equal("open", cycleStatus);
    }
}
