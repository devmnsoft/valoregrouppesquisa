extern alias ValoraWeb;

using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Valora.Infrastructure.Security;
using Valora.Tests.Support;
using WebProgram = ValoraWeb::Program;
using BffClient = ValoraWeb::Valora.Web.Services.Bff.BffApiClient;
using IBffClient = ValoraWeb::Valora.Web.Services.Bff.IBffApiClient;

namespace Valora.Tests;

public sealed class EvolutionAuthenticatedJourneyTests {
    [PostgresFact("jornada calculada com autenticação real não executada.")]
    public async Task Real_login_enforces_organizations_permissions_and_calculated_reevaluation() {
        var connection = PostgresTestEnvironment.ConnectionString("jornada autenticada");
        await using var db = new NpgsqlConnection(connection);
        var a = Guid.NewGuid(); var b = Guid.NewGuid();
        var baseline = Guid.NewGuid(); var posterior = Guid.NewGuid(); var foreign = Guid.NewGuid(); var incompatible = Guid.NewGuid();
        var platform = Guid.NewGuid(); var monitor = Guid.NewGuid();
        var manager = Guid.NewGuid(); var reader = Guid.NewGuid(); var outsider = Guid.NewGuid(); var otherManager = Guid.NewGuid();
        const string password = "Synthetic-Valora-2026!";
        var hash = new BCryptPasswordHasher().Hash(password);
        try {
            await SeedOrganization(db, a);
            await SeedOrganization(db, b);
            await SeedUser(db, a, manager, hash, "empresa_admin", ["evolution.read", "evolution.manage", "evolution.snapshots.generate"]);
            await SeedUser(db, a, platform, hash, "admin_valora", []);
            await SeedUser(db, a, monitor, hash, "gestor_area", ["evolution.read", "evolution.snapshots.generate"]);
            await SeedUser(db, a, reader, hash, "analista_resultados", ["evolution.read"]);
            await SeedUser(db, a, outsider, hash, "analista_resultados", []);
            await SeedUser(db, b, otherManager, hash, "empresa_admin", ["evolution.read", "evolution.manage"]);
            await SeedSurvey(db, a, baseline, 40, new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));
            await SeedSurvey(db, a, posterior, 70, new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc));
            await SeedSurvey(db, b, foreign, 90, new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc));

            await SeedSurvey(db, a, incompatible, 80, new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc), true);

            using var api = CreateApi(connection);
            using var apiClient = api.CreateClient();
            using var web = CreateWeb(connection, api);

            using var client = await Login(web, manager, password);
            var list = await client.GetStringAsync("/Evolution/Cycles");
            Assert.Contains("execution.cycles", list);
            SetCsrf(client, list);
            var csrf = client.DefaultRequestHeaders.GetValues("X-CSRF-TOKEN").Single();
            client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
            using (var wrongHeader = new HttpRequestMessage(HttpMethod.Post, $"/bff/diagnostics/{baseline}/workspace/nonexistent-command")) {
                wrongHeader.Headers.Add("RequestVerificationToken", csrf);
                Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(wrongHeader)).StatusCode);
            }
            SetCsrf(client, list);
            var protectedWorkspace = await client.PostAsync($"/bff/diagnostics/{baseline}/workspace/nonexistent-command", null);
            Assert.Equal(HttpStatusCode.MethodNotAllowed, protectedWorkspace.StatusCode);

            var options = await client.GetStringAsync("/Evolution/MeasurementOptions");
            Assert.Contains(baseline.ToString(), options);
            Assert.DoesNotContain(foreign.ToString(), options);
            var opened = await client.PostAsync("/Evolution/Cycles/Open", Form(new() {
                ["Title"] = "Diagnóstico com título igual",
                ["Summary"] = "Validar evolução observada",
                ["ReferenceMode"] = "calculated",
                ["SourceSurveyId"] = baseline.ToString(),
                ["BaselineScore"] = "99",
                ["PeriodStart"] = "2026-01-01",
                ["EvidenceSummary"] = "Origem selecionada conscientemente"
            }));
            Assert.Equal(HttpStatusCode.Redirect, opened.StatusCode);
            var details = opened.Headers.Location!.OriginalString;
            var cycle = await db.QuerySingleAsync<Guid>("SELECT id FROM valorapesquisa.evolution_cycles WHERE organization_id=@a", new { a });
            Assert.Equal(40, await db.QuerySingleAsync<decimal>("SELECT baseline_score FROM valorapesquisa.evolution_cycles WHERE id=@cycle", new { cycle }));

            var monitorCreated = await client.PostAsync("/Evolution/Cycles/Open", Form(new() {
                ["Title"] = "Acompanhamento restrito",
                ["Summary"] = "Validar responsável exclusivo",
                ["ReferenceMode"] = "manual",
                ["BaselineScore"] = "0",
                ["PeriodStart"] = "2026-01-01",
                ["EvidenceSummary"] = "Referência sintética declarada"
            }));
            Assert.Equal(HttpStatusCode.Redirect, monitorCreated.StatusCode);
            var monitorDetails = monitorCreated.Headers.Location!.OriginalString;
            var monitorCycle = await db.QuerySingleAsync<Guid>("SELECT id FROM valorapesquisa.evolution_cycles WHERE organization_id=@a AND title='Acompanhamento restrito'", new { a });
            using var monitoringClient = await Login(web, monitor, password);
            var monitoringPage = await monitoringClient.GetStringAsync(monitorDetails);
            Assert.Contains("/Snapshot?", monitoringPage);
            Assert.DoesNotContain("/Close?", monitoringPage);
            Assert.DoesNotContain("/Preview?", monitoringPage);
            SetCsrf(monitoringClient, monitoringPage);
            var monitorFields = new Dictionary<string, string> {
                ["Evidence"] = "Evidência registrada pelo responsável",
                ["Interpretation"] = "Observação sem inferir melhoria",
                ["Recommendation"] = "Revisar em conjunto"
            };
            Assert.Equal(HttpStatusCode.Redirect, (await monitoringClient.PostAsync($"/Evolution/Cycles/{monitorCycle}/Snapshot", Form(monitorFields))).StatusCode);
            var monitorDenied = await monitoringClient.PostAsync($"/Evolution/Cycles/{monitorCycle}/Close", Form(new() {
                ["version"] = "2",
                ["learning"] = "Não autorizado a encerrar",
                ["pendingTreatment"] = "Preservar situação"
            }));
            Assert.Contains("/error/403", monitorDenied.Headers.Location!.OriginalString);
            Assert.Equal("open", await db.QuerySingleAsync<string>("SELECT status FROM valorapesquisa.evolution_cycles WHERE id=@monitorCycle", new { monitorCycle }));
            Assert.Equal(1, await db.ExecuteScalarAsync<int>("SELECT count(*) FROM valorapesquisa.evolution_snapshots WHERE evolution_cycle_id=@monitorCycle AND created_by_user_id=@monitor", new { monitorCycle, monitor }));

            using var global = await Login(web, platform, password);
            Assert.Equal(HttpStatusCode.Forbidden, (await global.GetAsync("/Evolution")).StatusCode);
            SetCsrf(global, await global.GetStringAsync("/Account/Login"));
            var selectOther = await global.PostAsJsonAsync("/bff/auth/select-organization", new { organizationId = b, reason = "Homologação sintética de contexto" });
            Assert.True(selectOther.IsSuccessStatusCode, await selectOther.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.NotFound, (await global.GetAsync(details)).StatusCode);
            var selectOwn = await global.PostAsJsonAsync("/bff/auth/select-organization", new { organizationId = a, reason = "Homologação sintética de contexto" });
            Assert.True(selectOwn.IsSuccessStatusCode, await selectOwn.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, (await global.GetAsync(details)).StatusCode);

            using var queryClient = await Login(web, reader, password);
            var readPage = await queryClient.GetStringAsync(details);
            Assert.DoesNotContain("/Snapshot?", readPage);
            Assert.DoesNotContain("/Close?", readPage);
            Assert.DoesNotContain("/Preview?", readPage);
            SetCsrf(queryClient, readPage);
            var deniedCommand = await queryClient.PostAsync($"/Evolution/Cycles/{cycle}/Close", Form(new() { ["version"] = "1", ["learning"] = "Tentativa indevida", ["pendingTreatment"] = "Sem alteração" }));
            Assert.Equal(HttpStatusCode.Redirect, deniedCommand.StatusCode);
            Assert.Contains("/error/403", deniedCommand.Headers.Location!.OriginalString);
            using var noAccess = await Login(web, outsider, password);
            Assert.Contains("/error/403", (await noAccess.GetAsync("/Evolution")).Headers.Location!.OriginalString);
            using var other = await Login(web, otherManager, password);
            Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(details)).StatusCode);
            Assert.DoesNotContain(baseline.ToString(), await other.GetStringAsync("/Evolution/MeasurementOptions"));

            var preview = await Preview(client, cycle, posterior);
            Assert.Matches(@"\+30[.,]00", WebUtility.HtmlDecode(preview));
            var command = Confirmation(preview);
            await db.ExecuteAsync("UPDATE valorapesquisa.dimension_scores SET score=75 WHERE result_score_id IN (SELECT rs.id FROM valorapesquisa.result_scores rs JOIN valorapesquisa.responses r ON r.id=rs.response_id WHERE r.survey_id=@posterior)", new { posterior });
            var stale = await client.PostAsync($"/Evolution/Cycles/{cycle}/Reevaluate", Form(command));
            Assert.Equal(HttpStatusCode.OK, stale.StatusCode);
            Assert.Contains("mudou", WebUtility.HtmlDecode(await stale.Content.ReadAsStringAsync()));
            Assert.Equal(0, await db.ExecuteScalarAsync<int>("SELECT count(*) FROM valorapesquisa.evolution_snapshots WHERE evolution_cycle_id=@cycle", new { cycle }));
            preview = await Preview(client, cycle, posterior);
            command = Confirmation(preview);
            for (var attempt = 0; attempt < 2; attempt++) {
                var confirmed = await client.PostAsync($"/Evolution/Cycles/{cycle}/Reevaluate", Form(command));
                Assert.Equal(HttpStatusCode.Redirect, confirmed.StatusCode);
            }
            Assert.Equal(1, await db.ExecuteScalarAsync<int>("SELECT count(*) FROM valorapesquisa.evolution_snapshots WHERE evolution_cycle_id=@cycle", new { cycle }));
            Assert.Equal(35, await db.QuerySingleAsync<decimal>("SELECT delta FROM valorapesquisa.evolution_snapshots WHERE evolution_cycle_id=@cycle", new { cycle }));
            var monitoring = await client.PostAsync($"/Evolution/Cycles/{cycle}/Snapshot", Form(new() {
                ["Evidence"] = "Acompanhamento com autoria autenticada",
                ["Interpretation"] = "Execução não comprova melhoria",
                ["Recommendation"] = "Manter observação das limitações"
            }));
            Assert.Equal(HttpStatusCode.Redirect, monitoring.StatusCode);
            var incompatiblePreview = await Preview(client, cycle, incompatible);
            Assert.Contains("diferem escala", WebUtility.HtmlDecode(incompatiblePreview));
            var incompatibleConfirmation = await client.PostAsync($"/Evolution/Cycles/{cycle}/Reevaluate", Form(Confirmation(incompatiblePreview)));
            Assert.Equal(HttpStatusCode.Redirect, incompatibleConfirmation.StatusCode);
            Assert.Equal(3, await db.ExecuteScalarAsync<int>("SELECT count(*) FROM valorapesquisa.evolution_snapshots WHERE evolution_cycle_id=@cycle", new { cycle }));
            Assert.Equal(1, await db.ExecuteScalarAsync<int>("SELECT count(*) FROM valorapesquisa.evolution_snapshots WHERE evolution_cycle_id=@cycle AND snapshot_type='reevaluation' AND delta IS NULL", new { cycle }));
            Assert.Equal(75, await db.QuerySingleAsync<decimal>("SELECT current_score FROM valorapesquisa.evolution_cycles WHERE id=@cycle", new { cycle }));
            var reopened = await client.GetStringAsync(details);
            Assert.Matches(@"75[.,]00", reopened);
            Assert.Matches(@"40[.,]00", reopened);
            SetCsrf(client, reopened);
            var closed = await client.PostAsync($"/Evolution/Cycles/{cycle}/Close", Form(new() {
                ["version"] = Hidden(reopened, "version"),
                ["learning"] = "Variação observada sem causalidade",
                ["pendingTreatment"] = "Manter revisão das limitações"
            }));
            Assert.Equal(HttpStatusCode.Redirect, closed.StatusCode);
            Assert.Equal("closed", await db.QuerySingleAsync<string>("SELECT status FROM valorapesquisa.evolution_cycles WHERE id=@cycle", new { cycle }));
            Assert.Contains("Variação observada sem causalidade", WebUtility.HtmlDecode(await client.GetStringAsync(details)));

            var legacy = Guid.NewGuid();
            await db.ExecuteAsync("INSERT INTO valorapesquisa.evolution_cycles(id,organization_id,title,baseline_score,current_score,metadata_json) VALUES(@legacy,@a,'Synthetic historical reference',37.5,42,'{}'::jsonb)", new { legacy, a });
            var legacyDetails = $"/Evolution/Cycles/Details/{legacy}";
            var legacyPage = await client.GetStringAsync(legacyDetails);
            var review = new Dictionary<string, string> {
                ["version"] = Hidden(legacyPage, "version"),
                ["commandId"] = Hidden(legacyPage, "commandId"),
                ["evidence"] = "Arquivo histórico sem leitura verificável",
                ["nextStep"] = "Solicitar versão original da medição"
            };
            for (var attempt = 0; attempt < 2; attempt++) {
                Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync($"/Evolution/Cycles/{legacy}/ReviewReference", Form(review))).StatusCode);
            }
            Assert.Equal(1, await db.ExecuteScalarAsync<int>("SELECT count(*) FROM valorapesquisa.evolution_snapshots WHERE evolution_cycle_id=@legacy AND snapshot_type='reference_review'", new { legacy }));
            Assert.Equal(37.5m, await db.QuerySingleAsync<decimal>("SELECT baseline_score FROM valorapesquisa.evolution_cycles WHERE id=@legacy", new { legacy }));
            Assert.Contains("Solicitar versão original da medição", WebUtility.HtmlDecode(await client.GetStringAsync(legacyDetails)));

            await db.ExecuteAsync("UPDATE valorapesquisa.client_subscription_modules SET status='read_only' WHERE client_id=@a", new { a });
            var restricted = await client.PostAsync("/Evolution/Cycles/Open", Form(new()));
            Assert.Equal(HttpStatusCode.Forbidden, restricted.StatusCode);
            Assert.Contains("MODULE_READ_ONLY", await restricted.Content.ReadAsStringAsync());
            var globalReadOnly = await global.GetStringAsync(monitorDetails);
            Assert.DoesNotContain("/Snapshot?", globalReadOnly);
            SetCsrf(global, globalReadOnly);
            var globalDenied = await global.PostAsync($"/Evolution/Cycles/{monitorCycle}/Snapshot", Form(monitorFields));
            Assert.Contains("/error/403", globalDenied.Headers.Location!.OriginalString);
            Assert.Equal(1, await db.ExecuteScalarAsync<int>("SELECT count(*) FROM valorapesquisa.evolution_snapshots WHERE evolution_cycle_id=@monitorCycle", new { monitorCycle }));

        }
        finally {
            // Keep the synthetic evidence in the disposable test database, with accounts disabled.
            await db.ExecuteAsync("UPDATE valorapesquisa.users SET status='inactive',deleted_at=now() WHERE organization_id=ANY(@organizations); UPDATE valorapesquisa.organizations SET status='inactive',deleted_at=now() WHERE id=ANY(@organizations); UPDATE valorapesquisa.client_subscriptions SET status='cancelled' WHERE client_id=ANY(@organizations); UPDATE valorapesquisa.saas_customers SET status='blocked' WHERE organization_id=ANY(@organizations)", new { organizations = new[] { a, b } });
        }
    }

    [PostgresFact("preparação, publicação e campanha autenticadas não executadas.")]
    public async Task Published_qualitative_collection_and_manual_campaign_use_canonical_http_commands() {
        var connection = PostgresTestEnvironment.ConnectionString("preparação e campanha");
        await using var db = new NpgsqlConnection(connection);
        var organization = Guid.NewGuid(); var otherOrganization = Guid.NewGuid();
        var manager = Guid.NewGuid(); var reader = Guid.NewGuid(); var otherReader = Guid.NewGuid();
        const string password = "Synthetic-Valora-2026!";
        var hash = new BCryptPasswordHasher().Hash(password);
        try {
            await SeedOrganization(db, organization);
            await SeedOrganization(db, otherOrganization);
            await SeedUser(db, organization, manager, hash, "empresa_admin", ["forms.read", "forms.create", "forms.update", "forms.manage", "forms.publish", "surveys.read", "surveys.create", "surveys.update", "surveys.publish", "surveys.distribute", "campaigns.read", "campaigns.manage"]);
            await SeedUser(db, organization, reader, hash, "analista_resultados", ["surveys.read", "campaigns.read"]);
            await SeedUser(db, otherOrganization, otherReader, hash, "analista_resultados", ["campaigns.read"]);
            using var api = CreateApi(connection);
            using var apiClient = api.CreateClient();
            using var web = CreateWeb(connection, api);
            using var client = await Login(web, manager, password);
            SetCsrf(client, await client.GetStringAsync("/Account/Login"));
            var createdForm = await Json(await client.PostAsJsonAsync("/bff/forms", new {
                name = "Coleta qualitativa sintética",
                description = "Compreender oportunidades sem pontuar respostas textuais",
                category = "Qualitativo",
                estimatedMinutes = 5
            }));
            var form = createdForm.GetProperty("id").GetGuid();
            var draftVersion = createdForm.GetProperty("draftVersion").GetInt64();
            var initialReview = await Json(await client.GetAsync($"/bff/forms/{form}/publication-review"));
            Assert.False(initialReview.GetProperty("canPublish").GetBoolean());
            Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PostAsJsonAsync($"/bff/forms/{form}/publish", new { expectedVersion = draftVersion })).StatusCode);
            var section = await Json(await client.PostAsJsonAsync($"/bff/forms/{form}/sections", new { title = "Contexto", description = "Percepções qualitativas", position = 0, expectedVersion = draftVersion }));
            var detail = await Json(await client.GetAsync($"/bff/forms/{form}"));
            var originalQuestion = await Json(await client.PostAsJsonAsync($"/bff/forms/{form}/questions", new {
                sectionId = section.GetProperty("id").GetGuid(),
                code = "oportunidade",
                type = "long_text",
                title = "Que oportunidade deve ser considerada?",
                description = "Não inclua dados pessoais",
                required = true,
                dimensionCode = (string?)null,
                weight = 1,
                position = 0,
                settings = "{}",
                expectedVersion = detail.GetProperty("draftVersion").GetInt64()
            }));
            detail = await Json(await client.GetAsync($"/bff/forms/{form}"));
            var ready = await Json(await client.GetAsync($"/bff/forms/{form}/publication-review"));
            Assert.True(ready.GetProperty("canPublish").GetBoolean());
            Assert.NotEmpty(ready.GetProperty("warnings").EnumerateArray());
            var published = await Json(await client.PostAsJsonAsync($"/bff/forms/{form}/publish", new { expectedVersion = detail.GetProperty("draftVersion").GetInt64() }));
            Assert.Equal("published", published.GetProperty("status").GetString());
            using var queryClient = await Login(web, reader, password);
            SetCsrf(queryClient, await queryClient.GetStringAsync("/Account/Login"));
            Assert.Equal(HttpStatusCode.Forbidden, (await queryClient.PostAsJsonAsync("/bff/surveys", new { })).StatusCode);
            var survey = await CreateCollection(client, form, published.GetProperty("id").GetGuid(), "Coleta manual");
            Assert.Equal(HttpStatusCode.Forbidden, (await queryClient.PatchAsJsonAsync($"/bff/surveys/{survey}/status", new { status = "active" })).StatusCode);
            await Json(await client.PatchAsJsonAsync($"/bff/surveys/{survey}/status", new { status = "active" }));
            var campaignPath = $"/bff/diagnostics/{survey}/campaign";
            var created = await Json(await client.PostAsJsonAsync(campaignPath, new { name = "Convite manual", message = "Participe da coleta qualitativa", channel = "manual", recipients = Array.Empty<object>() }));
            Assert.Equal(System.Text.Json.JsonValueKind.Null, created.GetProperty("completionRate").ValueKind);
            var readOnlyCampaign = await queryClient.GetAsync(campaignPath);
            Assert.Equal(HttpStatusCode.OK, readOnlyCampaign.StatusCode);
            Assert.Equal("false", readOnlyCampaign.Headers.GetValues("X-Valora-Campaign-Manage").Single());
            Assert.Equal("false", readOnlyCampaign.Headers.GetValues("X-Valora-Campaign-Distribute").Single());

            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(campaignPath + "/send", new { expectedVersion = 1 })).StatusCode);
            var link = await Json(await client.PostAsJsonAsync($"/bff/surveys/{survey}/links", new { }));
            using var anonymous = web.CreateClient(new() { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
            Assert.Equal(HttpStatusCode.Redirect, (await anonymous.GetAsync(link.GetProperty("publicUrl").GetString())).StatusCode);
            var publicSurvey = await anonymous.GetAsync($"/bff/public/surveys/{survey}");
            Assert.Equal(HttpStatusCode.OK, publicSurvey.StatusCode);
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, (await client.PostAsync(campaignPath + "/send", null)).StatusCode);
            var activated = await Json(await client.PostAsJsonAsync(campaignPath + "/send", new { expectedVersion = 1 }));
            Assert.Equal("active", activated.GetProperty("status").GetString());
            Assert.Null(await db.QuerySingleAsync<DateTime?>("SELECT sent_at FROM valorapesquisa.diagnostic_campaigns WHERE survey_id=@survey AND organization_id=@organization", new { survey, organization }));

            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(campaignPath + "/cancel", new { expectedVersion = 1 })).StatusCode);
            var reopened = await Json(await client.GetAsync(campaignPath));
            Assert.Equal("manual", reopened.GetProperty("channel").GetString());
            Assert.Equal(0, reopened.GetProperty("sentCount").GetInt32());
            Assert.Equal(0, reopened.GetProperty("queuedCount").GetInt32());
            Assert.Equal(System.Text.Json.JsonValueKind.Null, reopened.GetProperty("completionRate").ValueKind);
            using var other = await Login(web, otherReader, password);
            Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(campaignPath)).StatusCode);
            var competing = await Task.WhenAll(
                client.PostAsJsonAsync(campaignPath + "/cancel", new { expectedVersion = reopened.GetProperty("version").GetInt64() }),
                client.PostAsJsonAsync(campaignPath + "/cancel", new { expectedVersion = reopened.GetProperty("version").GetInt64() }));
            Assert.Single(competing, response => response.IsSuccessStatusCode);
            Assert.Single(competing, response => response.StatusCode == HttpStatusCode.Conflict);
            var cancelled = await Json(competing.Single(response => response.IsSuccessStatusCode));
            Assert.Equal("cancelled", cancelled.GetProperty("status").GetString());
            foreach (var response in competing) response.Dispose();
            Assert.True(await db.ExecuteScalarAsync<bool>("SELECT count(*)=0 FROM valorapesquisa.email_jobs WHERE organization_id=@organization", new { organization }));
            var whatsapp = await CreateCollection(client, form, published.GetProperty("id").GetGuid(), "Coleta WhatsApp manual");
            await Json(await client.PatchAsJsonAsync($"/bff/surveys/{whatsapp}/status", new { status = "active" }));
            await Json(await client.PostAsJsonAsync($"/bff/surveys/{whatsapp}/links", new { }));
            var whatsappPath = $"/bff/diagnostics/{whatsapp}/campaign";
            var whatsappCampaign = await Json(await client.PostAsJsonAsync(whatsappPath, new { name = "Convite WhatsApp", message = "Compartilhamento manual", channel = "whatsapp_manual", recipients = Array.Empty<object>() }));
            Assert.Equal("whatsapp_manual", whatsappCampaign.GetProperty("channel").GetString());
            Assert.Equal("active", (await Json(await client.PostAsJsonAsync(whatsappPath + "/send", new { expectedVersion = 1 }))).GetProperty("status").GetString());
            var emailSurvey = await CreateCollection(client, form, published.GetProperty("id").GetGuid(), "Coleta e-mail");
            await Json(await client.PatchAsJsonAsync($"/bff/surveys/{emailSurvey}/status", new { status = "active" }));
            var emailPath = $"/bff/diagnostics/{emailSurvey}/campaign";
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(emailPath, new { name = "Convite e-mail", message = "Convite sintético", channel = "email", subject = "Convite", recipients = new[] { new { email = "same@valora.test", hasConsent = true }, new { email = "SAME@VALORA.TEST", hasConsent = true } } })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(emailPath, new { name = "Convite e-mail", message = "Convite sintético", channel = "email", subject = "Convite", recipients = new[] { new { email = "invalid", hasConsent = true } } })).StatusCode);
            var emailCampaign = await Json(await client.PostAsJsonAsync(emailPath, new { name = "Convite e-mail", message = "Convite sintético", channel = "email", subject = "Convite", recipients = new[] { new { email = "synthetic@valora.test", hasConsent = true } } }));
            Assert.Equal(1, emailCampaign.GetProperty("recipientCount").GetInt32());
            Assert.Equal(0, emailCampaign.GetProperty("queuedCount").GetInt32());
            Assert.Equal(0, emailCampaign.GetProperty("sentCount").GetInt32());
            var emailBlocked = await client.PostAsJsonAsync(emailPath + "/send", new { expectedVersion = 1 });
            Assert.Equal(HttpStatusCode.Conflict, emailBlocked.StatusCode);
            Assert.Contains("configurado", await emailBlocked.Content.ReadAsStringAsync());
            Assert.Equal("draft", (await Json(await client.GetAsync(emailPath))).GetProperty("status").GetString());
            Assert.Equal(1, await db.ExecuteScalarAsync<int>("SELECT count(*) FROM valorapesquisa.email_jobs WHERE organization_id=@organization AND next_attempt_at>now()+interval '100 years'", new { organization }));

            detail = await Json(await client.GetAsync($"/bff/forms/{form}"));
            var newDraft = await Json(await client.PostAsJsonAsync($"/bff/forms/{form}/versions", new { expectedFormVersion = detail.GetProperty("version").GetInt64() }));
            var newQuestion = newDraft.GetProperty("sections")[0].GetProperty("questions")[0];
            var draftSection = newDraft.GetProperty("sections")[0];
            var updatedSection = await Json(await client.PutAsJsonAsync($"/bff/forms/{form}/sections/{draftSection.GetProperty("id").GetGuid()}", new {
                title = "Seção revisada no rascunho",
                description = "Sem alterar a coleta publicada",
                expectedVersion = draftSection.GetProperty("version").GetInt64()
            }));

            Assert.NotEqual(originalQuestion.GetProperty("id").GetGuid(), newQuestion.GetProperty("id").GetGuid());
            await Json(await client.PutAsJsonAsync($"/bff/forms/{form}/questions/{newQuestion.GetProperty("id").GetGuid()}", new {
                code = "oportunidade",
                type = "long_text",
                title = "Texto alterado somente no rascunho",
                description = "Sem alterar a versão publicada",
                required = true,
                dimensionCode = (string?)null,
                weight = 1,
                settings = "{}",
                expectedVersion = newQuestion.GetProperty("version").GetInt64()
            }));
            detail = await Json(await client.GetAsync($"/bff/forms/{form}"));
            var temporaryQuestion = await Json(await client.PostAsJsonAsync($"/bff/forms/{form}/questions", new {
                sectionId = draftSection.GetProperty("id").GetGuid(),
                code = "temporaria",
                type = "single_choice",
                title = "Pergunta temporária para validar edição",
                description = "Somente rascunho",
                required = false,
                dimensionCode = (string?)null,
                weight = 1,
                position = 1,
                settings = "{}",
                expectedVersion = detail.GetProperty("draftVersion").GetInt64()
            }));
            detail = await Json(await client.GetAsync($"/bff/forms/{form}"));
            var option = await Json(await client.PostAsJsonAsync($"/bff/forms/{form}/questions/{temporaryQuestion.GetProperty("id").GetGuid()}/options", new {
                label = "Opção temporária",
                value = "opcao",
                score = 1,
                position = 0,
                expectedVersion = detail.GetProperty("draftVersion").GetInt64()
            }));
            option = await Json(await client.PutAsJsonAsync($"/bff/forms/{form}/options/{option.GetProperty("id").GetGuid()}", new {
                label = "Opção revisada",
                value = "opcao",
                score = 2,
                expectedVersion = option.GetProperty("version").GetInt64()
            }));
            await DeleteDraftItem(client, $"/bff/forms/{form}/options/{option.GetProperty("id").GetGuid()}", option.GetProperty("version").GetInt64());
            await DeleteDraftItem(client, $"/bff/forms/{form}/questions/{temporaryQuestion.GetProperty("id").GetGuid()}", temporaryQuestion.GetProperty("version").GetInt64());
            await DeleteDraftItem(client, $"/bff/forms/{form}/sections/{draftSection.GetProperty("id").GetGuid()}", updatedSection.GetProperty("version").GetInt64());
            var preservedPublic = await Json(await anonymous.GetAsync($"/bff/public/surveys/{survey}"));
            Assert.Equal(originalQuestion.GetProperty("id").GetGuid(), preservedPublic.GetProperty("form").GetProperty("questions")[0].GetProperty("id").GetGuid());
            Assert.DoesNotContain("Texto alterado somente no rascunho", preservedPublic.GetRawText());
            Assert.Equal(published.GetProperty("id").GetGuid(), await db.QuerySingleAsync<Guid>("SELECT form_version_id FROM valorapesquisa.surveys WHERE id=@survey AND organization_id=@organization", new { survey, organization }));
            await db.ExecuteAsync("UPDATE valorapesquisa.client_subscription_modules csm SET status='read_only' FROM valorapesquisa.saas_modules m WHERE csm.module_id=m.id AND csm.client_id=@organization AND m.code='surveys'", new { organization });
            var contractDenied = await client.PostAsJsonAsync($"/bff/surveys/{survey}/links", new { });
            Assert.Equal(HttpStatusCode.Forbidden, contractDenied.StatusCode);
            Assert.Contains("MODULE_READ_ONLY", await contractDenied.Content.ReadAsStringAsync());
            var contractedReadOnly = await client.GetAsync(campaignPath);
            Assert.Equal(HttpStatusCode.OK, contractedReadOnly.StatusCode);
            Assert.Equal("read-only", contractedReadOnly.Headers.GetValues("X-Valora-Module-Access").Single());

            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(campaignPath + "/cancel", new { expectedVersion = 3 })).StatusCode);
            await db.ExecuteAsync("UPDATE valorapesquisa.client_subscription_modules csm SET status='read_only' FROM valorapesquisa.saas_modules m WHERE csm.module_id=m.id AND csm.client_id=@organization AND m.code='forms'", new { organization });
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"/bff/forms/{form}/publish", new { expectedVersion = 1 })).StatusCode);

        }
        finally {
            await db.ExecuteAsync("UPDATE valorapesquisa.users SET status='inactive',deleted_at=now() WHERE organization_id=ANY(@organizations); UPDATE valorapesquisa.organizations SET status='inactive',deleted_at=now() WHERE id=ANY(@organizations); UPDATE valorapesquisa.client_subscriptions SET status='cancelled' WHERE client_id=ANY(@organizations); UPDATE valorapesquisa.saas_customers SET status='blocked' WHERE organization_id=ANY(@organizations)", new { organizations = new[] { organization, otherOrganization } });
        }
    }
    private static async Task DeleteDraftItem(HttpClient client, string path, long version) {
        using var request = new HttpRequestMessage(HttpMethod.Delete, path) { Content = JsonContent.Create(new { expectedVersion = version }) };
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
    private static async Task<Guid> CreateCollection(HttpClient client, Guid form, Guid version, string title) {
        var result = await Json(await client.PostAsJsonAsync("/bff/surveys", new { formId = form, formVersionId = version, title, description = "Objetivo sintético de coleta qualitativa" }));
        return result.GetProperty("id").GetGuid();
    }
    private static async Task<System.Text.Json.JsonElement> Json(HttpResponseMessage response) {
        using (response) {
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, $"HTTP {(int)response.StatusCode}: {body}");
            using var document = System.Text.Json.JsonDocument.Parse(body);
            return document.RootElement.Clone();
        }
    }

    private static WebApplicationFactory<Program> CreateApi(string connection) => new WebApplicationFactory<Program>().WithWebHostBuilder(builder => {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Email:Enabled", "false");
        ConfigureDatabase(builder, connection);
        builder.UseSetting("Jwt:SigningKey", "SYNTHETIC_TEST_KEY_ONLY_2026_MORE_THAN_32_CHARACTERS");
        builder.UseSetting("Jwt:Issuer", "Valora.Tests");
        builder.UseSetting("Jwt:Audience", "Valora.Tests");
    });
    private static WebApplicationFactory<WebProgram> CreateWeb(string connection, WebApplicationFactory<Program> api) => new WebApplicationFactory<WebProgram>().WithWebHostBuilder(builder => {
        builder.UseEnvironment("Development");
        ConfigureDatabase(builder, connection);
        builder.UseSetting("Api:BaseUrl", "http://localhost");
        builder.ConfigureServices(services => services.AddHttpClient<IBffClient, BffClient>()
            .ConfigurePrimaryHttpMessageHandler(() => api.Server.CreateHandler()));
    });

    private static void ConfigureDatabase(IWebHostBuilder builder, string connection) {
        builder.UseSetting("ConnectionStrings:Postgres", connection);
        builder.UseSetting("ConnectionStrings:DefaultConnection", connection);
    }
    private static async Task<HttpClient> Login(WebApplicationFactory<WebProgram> web, Guid user, string password) {
        var client = web.CreateClient(new() { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        SetCsrf(client, await client.GetStringAsync("/Account/Login"));
        var login = await client.PostAsJsonAsync("/bff/auth/login", new { email = $"{user:N}@valora.test", password });
        Assert.True(login.IsSuccessStatusCode, $"Login HTTP retornou {(int)login.StatusCode}: {await login.Content.ReadAsStringAsync()}");
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        return client;
    }
    private static void SetCsrf(HttpClient client, string html) {
        var token = Regex.Match(html, "name=\"csrf-token\" content=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", WebUtility.HtmlDecode(token));
    }
    private static string Hidden(string html, string name) {
        var value = Regex.Match(html, $"name=\"{name}\" value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(value);
        return WebUtility.HtmlDecode(value);
    }
    private static Dictionary<string, string> Confirmation(string html) => new() {
        ["sourceSurveyId"] = Hidden(html, "sourceSurveyId"),
        ["version"] = Hidden(html, "version"),
        ["fingerprint"] = Hidden(html, "fingerprint"),
        ["commandId"] = Hidden(html, "commandId")
    };
    private static FormUrlEncodedContent Form(Dictionary<string, string> fields) => new(fields);
    private static async Task<string> Preview(HttpClient client, Guid cycle, Guid survey) {
        var response = await client.PostAsync($"/Evolution/Cycles/{cycle}/Preview", Form(new() { ["sourceSurveyId"] = survey.ToString() }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }
    private static async Task SeedOrganization(NpgsqlConnection db, Guid org) {
        await db.ExecuteAsync("""
INSERT INTO valorapesquisa.organizations(id,name,slug) VALUES(@org,'Synthetic authenticated journey',@slug);
INSERT INTO valorapesquisa.saas_customers(organization_id,legal_name,trade_name,tax_id_normalized,plan_code) VALUES(@org,'Synthetic authenticated journey','Synthetic authenticated journey',@syntheticTaxId,'enterprise');
INSERT INTO valorapesquisa.subscriptions(organization_id,plan_id,status) SELECT @org,id,'active' FROM valorapesquisa.plans WHERE code='enterprise';
INSERT INTO valorapesquisa.organization_modules(organization_id,module_id,module_code,enabled) SELECT @org,id,code,true FROM valorapesquisa.modules WHERE code IN ('organizational_intelligence','surveys','forms','distribution');
INSERT INTO valorapesquisa.client_subscriptions(client_id,plan_id,status) SELECT @org,id,'active' FROM valorapesquisa.saas_plans WHERE code='enterprise';
INSERT INTO valorapesquisa.client_subscription_modules(subscription_id,client_id,module_id,status) SELECT cs.id,@org,m.id,'active' FROM valorapesquisa.client_subscriptions cs CROSS JOIN valorapesquisa.saas_modules m WHERE cs.client_id=@org AND m.code IN ('evolution','surveys','forms');
""", new { org, slug = org.ToString("N"), syntheticTaxId = (BitConverter.ToUInt64(org.ToByteArray(), 0) % 100000000000000UL).ToString("D14") });
    }
    private static async Task SeedUser(NpgsqlConnection db, Guid org, Guid user, string hash, string baseRole, string[] permissions) {
        var role = Guid.NewGuid();
        await db.ExecuteAsync("""
INSERT INTO valorapesquisa.users(id,organization_id,name,email,password_hash,status) VALUES(@user,@org,'Synthetic user',@email,@hash,'active');
INSERT INTO valorapesquisa.user_roles(user_id,role_id) SELECT @user,id FROM valorapesquisa.roles WHERE code=@baseRole AND organization_id IS NULL;
INSERT INTO valorapesquisa.roles(id,organization_id,code,name) VALUES(@role,@org,@roleCode,'Synthetic explicit permissions');
INSERT INTO valorapesquisa.role_permissions(role_id,permission_id) SELECT @role,id FROM valorapesquisa.permissions WHERE code=ANY(@permissions);
INSERT INTO valorapesquisa.user_roles(user_id,role_id) VALUES(@user,@role);
""", new { org, user, hash, baseRole, role, roleCode = "qa_" + role.ToString("N"), permissions, email = $"{user:N}@valora.test" });
    }
    private static async Task SeedSurvey(NpgsqlConnection db, Guid org, Guid survey, decimal score, DateTime reference, bool differentScale = false) {
        var response = Guid.NewGuid(); var result = Guid.NewGuid();
        var snapshot = """{"methodology":{"code":"professional","version":3},"questions":[{"dimension":"people","scale":{"min":1,"max":5}}]}""";
        if (differentScale) snapshot = snapshot.Replace("\"max\":5", "\"max\":10");
        await db.ExecuteAsync("""
INSERT INTO valorapesquisa.surveys(id,organization_id,form_version_id,name,title,status,methodology_snapshot_hash,methodology_snapshot_json) SELECT @survey,@org,id,'Identical title','Identical title','active','synthetic-fixture',CAST(@snapshot AS jsonb) FROM valorapesquisa.form_versions ORDER BY created_at LIMIT 1;
INSERT INTO valorapesquisa.survey_methodology_question_snapshots(survey_id,question_id,methodology_version_id,dimension_code,metric_code,index_code,weight,polarity,calculation_rule,snapshot_hash,captured_at) SELECT @survey,q.id,m.id,'people','trust','maturity',1,1,'{}'::jsonb,'synthetic-fixture',@reference FROM valorapesquisa.questions q CROSS JOIN valorapesquisa.methodology_versions m ORDER BY q.id,m.id LIMIT 1;
INSERT INTO valorapesquisa.responses(id,organization_id,survey_id,participant_email,submitted_at) VALUES(@response,@org,@survey,'same-cohort@valora.test',@reference);
INSERT INTO valorapesquisa.result_scores(id,response_id,total_score,max_score,created_at,updated_at) VALUES(@result,@response,@score,100,@reference,@reference);
INSERT INTO valorapesquisa.dimension_scores(result_score_id,dimension_id,score,max_score) SELECT @result,id,@score,100 FROM valorapesquisa.dimensions WHERE code='people';
""", new { org, survey, response, result, score, reference, snapshot });
    }
}
