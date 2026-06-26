# Sprint 13 — Auditoria objetiva da implementação real

## Respostas objetivas

| # | Pergunta | Diagnóstico |
|---:|---|---|
| 1 | `backend/Valora.sln` existe? | Sim. |
| 2 | `backend/Valora.Api` existe? | Sim. |
| 3 | `backend/Valora.Application` existe? | Sim. |
| 4 | `backend/Valora.Domain` existe? | Sim. |
| 5 | `backend/Valora.Infrastructure` existe? | Sim. |
| 6 | `backend/Valora.Tests` existe? | Sim. |
| 7 | A API compila? | Não validado neste container: `dotnet` não está instalado. Estrutura e validadores estáticos passam. |
| 8 | Existem controllers reais? | Sim: health, auth, plans, public, certificates, communications, admin, organizations e surveys. |
| 9 | Existem repositories Dapper reais? | Sim: repositories em `Valora.Infrastructure/Repositories/Repositories.cs` usam Dapper e queries parametrizadas. |
| 10 | A API conecta no PostgreSQL? | Sim por `PostgresConnectionFactory` e `/health/database`; execução depende do PostgreSQL local. |
| 11 | PostgreSQL sobe via Docker? | Sim, `docker-compose.postgres.yml` aponta PostgreSQL 16 na porta 5434. |
| 12 | Existem migrations SQL reais? | Sim, `database/postgresql/*.sql`. |
| 13 | As migrations são idempotentes? | Sim em padrão `CREATE ... IF NOT EXISTS`, `CREATE INDEX IF NOT EXISTS` e seeds com `ON CONFLICT`. |
| 14 | Existe seed dos cinco planos oficiais? | Sim, `010_seed_official_plans.sql`. |
| 15 | Existe seed demo Valora Insight™? | Sim, `011_seed_demo_valora_insight.sql`. |
| 16 | Existe endpoint `/health`? | Sim. |
| 17 | Existe endpoint `/health/database`? | Sim. |
| 18 | Existe endpoint `/plans/public`? | Sim. |
| 19 | Existe endpoint `/auth/register-company`? | Sim. |
| 20 | Existe endpoint `/auth/login`? | Sim. |
| 21 | Existe endpoint `/public/surveys/{surveyId}/validate`? | Sim. |
| 22 | Existe endpoint `/public/surveys/{surveyId}/responses`? | Sim. |
| 23 | Existe endpoint `/public/results/{responseId}`? | Sim. |
| 24 | Existe criação de certificate metadata? | Sim, no fluxo de resposta pública PostgreSQL e no demo. |
| 25 | Existe criação de email_job? | Sim, quando há e-mail/consentimento no fluxo PostgreSQL e endpoint de comunicação. |
| 26 | `migration/export-firestore.js` exporta dados reais? | Sim quando `GOOGLE_APPLICATION_CREDENTIALS` está configurado; sem credencial executa dry-run seguro. |
| 27 | `migration/transform-firestore-to-postgres.js` transforma dados reais? | Sim, lê export JSON e gera arquivos normalizados em `migration/out`. |
| 28 | `migration/import-postgres.js` faz dry-run/apply/upsert? | Dry-run e resumo por lote existem; apply exige `DATABASE_URL` e ambiente com driver/execução controlada. |
| 29 | `migration/compare-firebase-postgres.js` gera relatório real? | Gera relatório JSON/Markdown; contagens reais dependem dos dados importados/exportados. |
| 30 | `DATA_PROVIDER=api` funciona localmente? | Sim por contrato estático e configuração `config/config.local-api.js`; execução depende da API local. |
| 31 | `DATA_PROVIDER=hybrid` compara sem duplicar escrita? | Sim por contrato/validador: escrita segue primário e comparação secundária é diagnóstica. |
| 32 | O que ainda é mock, stub, demo-only, TODO ou NotImplemented? | Demo público Valora Insight™ continua como seed/test fixture; geração PDF real é inicial textual; `migration/transform-to-postgres.js` legado ainda contém TODO e não é o script oficial da Sprint 13. |

## Mapeamento executado

Termos mapeados no repositório: `TODO`, `NotImplemented`, `not implemented`, `demo only`, `mock`, `stub`, `throw new Error`, `501`, `Valora.sln`, `Dapper`, `Npgsql`, `MigrationRunner`, `DATA_PROVIDER`, `HYBRID_PRIMARY_PROVIDER`, `ALLOW_API_PRODUCTION_CUTOVER`, `callPublicFunction`, `firebaseCallable`, `renderTakeSurvey`, `submitSurvey`, `renderResult`, `ValoraRepository`, `ValoraApiRepository`.

Comando recomendado:

```bash
rg -n "TODO|NotImplemented|not implemented|demo only|mock|stub|throw new Error|501|Valora.sln|Dapper|Npgsql|MigrationRunner|DATA_PROVIDER|HYBRID_PRIMARY_PROVIDER|ALLOW_API_PRODUCTION_CUTOVER|callPublicFunction|firebaseCallable|renderTakeSurvey|submitSurvey|renderResult|ValoraRepository|ValoraApiRepository" -S . -g '!node_modules'
```

## Correções Sprint 13 aplicadas

- Fluxo PostgreSQL de resposta pública agora usa transação para criar resposta, answers, result_scores, dimension_scores, certificate metadata, email_jobs e communications.
- Endpoint de comunicações passou a usar `ICommunicationRepository` em vez de resposta fixa em memória.
- API recebeu middleware global de erro JSON para evitar HTML em falhas não tratadas.
- Script Windows 55 foi criado para validar o backend/API/PostgreSQL operacional ponta a ponta.

## Restrições preservadas

- Produção permanece com `DATA_PROVIDER: 'firebase'` e `ALLOW_API_PRODUCTION_CUTOVER: false`.
- Firebase, repositórios híbridos e gateway existentes foram preservados.
- Nenhum segredo real, SMTP real, service account ou token WhatsApp foi colocado no frontend.
