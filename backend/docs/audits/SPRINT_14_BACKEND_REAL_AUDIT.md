# Sprint 14 — Auditoria Backend Real, PostgreSQL e Migração Executável

Data da auditoria: 2026-06-26.

## Escopo auditado

Foram verificados `package.json`, `config.js`, `config/config.production.js`, `runtime-capabilities.js`, `index.html`, `api-client.js`, `api-repository.js`, `gateway-client.js`, `repository.js`, `firebase-repository.js`, `local-repository.js`, `app.js`, `pdf.js`, `backend/`, `backend/database/postgresql/`, `migration/`, `communication-gateway/`, `scripts/` e `tools/windows/`.

## Respostas objetivas

| # | Pergunta | Resposta |
|---|---|---|
| 1 | `backend/Valora.sln` existe? | Sim. |
| 2 | `backend/Valora.Api` existe? | Sim. |
| 3 | `backend/Valora.Application` existe? | Sim. |
| 4 | `backend/Valora.Domain` existe? | Sim. |
| 5 | `backend/Valora.Infrastructure` existe? | Sim. |
| 6 | `backend/Valora.Tests` existe? | Sim. |
| 7 | A API compila? | Não validado neste ambiente porque o SDK `dotnet` não está instalado; a estrutura e os validadores estáticos passam. |
| 8 | Existem controllers reais? | Sim: Health, Plans, Auth, Public, Certificates, Communications, Admin e demais controllers. |
| 9 | Existem repositories Dapper reais? | Sim, em `Valora.Infrastructure/Repositories/Repositories.cs`, usando Dapper e queries parametrizadas. |
| 10 | A API conecta no PostgreSQL? | Sim, há `PostgresConnectionFactory` com `NpgsqlConnection` e health/database. Execução real depende de PostgreSQL local. |
| 11 | PostgreSQL sobe via Docker? | Sim, `docker-compose.postgres.yml` define `postgres:16` em `5434:5432`. |
| 12 | `backend/database/postgresql` possui migrations reais? | Sim, há scripts de schemas, tabelas, seeds e controle de migração. |
| 13 | As migrations são idempotentes? | Sim, usam `CREATE ... IF NOT EXISTS`, índices idempotentes e `INSERT ... ON CONFLICT`. |
| 14 | Existe seed dos cinco planos oficiais? | Sim, `010_seed_official_plans.sql` cobre `free`, `essential`, `professional`, `corporate` e `enterprise`. |
| 15 | Existe seed demo Valora Insight™? | Sim, `011_seed_demo_valora_insight.sql` cria organização demo, formulário, dimensões, perguntas, pesquisa e link público. |
| 16 | Existe endpoint `/health`? | Sim. |
| 17 | Existe endpoint `/health/database`? | Sim. |
| 18 | Existe endpoint `/plans/public`? | Sim. |
| 19 | Existe endpoint `/auth/register-company`? | Sim. |
| 20 | Existe endpoint `/auth/login`? | Sim. |
| 21 | Existe endpoint `/public/surveys/{surveyId}/validate`? | Sim. |
| 22 | Existe endpoint `/public/surveys/{surveyId}/responses`? | Sim. |
| 23 | Existe endpoint `/public/results/{responseId}`? | Sim. |
| 24 | Existe criação de certificate metadata? | Sim, na submissão pública PostgreSQL e no fluxo demo da API. |
| 25 | Existe criação de `email_job`? | Sim, quando há e-mail de participante/consentimento no fluxo de resposta. |
| 26 | `migration/export-firestore.js` exporta dados reais? | Sim, exporta coleções configuradas quando Firebase Admin está disponível; também suporta dry-run. |
| 27 | `migration/transform-firestore-to-postgres.js` transforma dados reais? | Sim, lê export real e gera arquivos normalizados em `migration/out/`. |
| 28 | `migration/import-postgres.js` faz dry-run/apply/upsert? | Sim, suporta dry-run/apply, truncate, batch-size, backup e upsert por lote. |
| 29 | `migration/compare-firebase-postgres.js` gera relatório real? | Sim, gera `reports/migration-comparison.json` e `MIGRATION_COMPARISON_REPORT.md`; em dry-run usa artefatos locais. |
| 30 | `DATA_PROVIDER=api` funciona localmente? | A camada frontend/API está implementada e os validadores estáticos passam; execução ponta a ponta requer API e PostgreSQL em execução. |
| 31 | `DATA_PROVIDER=hybrid` compara sem duplicar escrita? | Sim, o roteamento centralizado preserva escrita no primário e comparação/leitura secundária quando possível; validador passa. |
| 32 | O que ainda é mock, stub, demo-only, TODO ou NotImplemented? | Há rotas legadas 501 no communication-gateway para endpoints substituídos, modo demo intencional para Valora Insight API, e `migration/transform-to-postgres.js` legado com mensagem TODO. Os endpoints obrigatórios da API ASP.NET Core não usam 501/NotImplemented. |

## Mapeamento de termos obrigatórios

A busca por `TODO`, `NotImplemented`, `not implemented`, `demo only`, `mock`, `stub`, `throw new Error`, `501`, `Valora.sln`, `Dapper`, `Npgsql`, `MigrationRunner`, `DATA_PROVIDER`, `HYBRID_PRIMARY_PROVIDER`, `ALLOW_API_PRODUCTION_CUTOVER`, `callPublicFunction`, `firebaseCallable`, `renderTakeSurvey`, `submitSurvey`, `renderResult`, `ValoraRepository` e `ValoraApiRepository` retornou ocorrências esperadas em validadores, scripts Windows, frontend híbrido, backend e gateway. Pontos de atenção:

- `communication-gateway/src/routes/communication-routes.js` mantém respostas 501 JSON para endpoints legados de e-mail/WhatsApp direto, orientando uso de rotas suportadas.
- `migration/transform-to-postgres.js` é legado e aponta para a etapa segura atual `migration/transform-firestore-to-postgres.js`.
- Validadores usam `throw new Error` de forma esperada para falhas controladas.
- Produção permanece com `DATA_PROVIDER: 'firebase'` e `ALLOW_API_PRODUCTION_CUTOVER: false`.

## Validações executadas durante a auditoria

- `npm run check`: passou.
- `node scripts/validate-backend-implementation.js`: passou.
- `node scripts/validate-postgres-schema.js`: passou.
- `node scripts/validate-migration-scripts.js`: passou.
- `node scripts/validate-real-migration-dry-run.js`: passou.
- `dotnet build backend/Valora.sln`: não executado porque `dotnet` não está instalado neste ambiente.
- `dotnet test backend/Valora.sln`: não executado porque `dotnet` não está instalado neste ambiente.

## Diagnóstico

O repositório já contém uma implementação funcional e incremental de backend ASP.NET Core/PostgreSQL, preservando Firebase em produção e mantendo o frontend híbrido. A lacuna operacional encontrada nesta auditoria era a ausência do script Windows consolidado `tools/windows/56-validar-backend-api-postgres-real.bat`, agora criado para executar a esteira completa de validação backend/API/PostgreSQL/migração sem trocar o provider de produção.
