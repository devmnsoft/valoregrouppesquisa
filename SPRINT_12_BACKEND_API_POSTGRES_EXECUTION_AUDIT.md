# Sprint 12 — Backend/API/PostgreSQL Operational Execution Audit

Data da auditoria: 2026-06-26.

## Escopo auditado

Arquivos e diretórios auditados: `package.json`, `config.js`, `config/config.production.js`, `runtime-capabilities.js`, `index.html`, `api-client.js`, `api-repository.js`, `gateway-client.js`, `repository.js`, `firebase-repository.js`, `local-repository.js`, `app.js`, `pdf.js`, `backend/`, `database/postgresql/`, `migration/`, `communication-gateway/`, `scripts/` e `tools/windows/`.

## Respostas objetivas obrigatórias

| # | Pergunta | Resposta |
|---|---|---|
| 1 | `backend/Valora.sln` existe? | Sim. |
| 2 | `backend/Valora.Api` existe? | Sim. |
| 3 | `backend/Valora.Application` existe? | Sim. |
| 4 | `backend/Valora.Domain` existe? | Sim. |
| 5 | `backend/Valora.Infrastructure` existe? | Sim. |
| 6 | `backend/Valora.Tests` existe? | Sim. |
| 7 | A API compila? | Não validado neste container porque `dotnet` não está instalado; o script `backend:build` está configurado. |
| 8 | A API tem controllers reais? | Sim: Health, Auth, Plans, Public, Certificates, Communications, Admin, Organizations e Surveys. |
| 9 | A API tem repositories Dapper reais? | Sim, em `Valora.Infrastructure/Repositories/Repositories.cs`, usando Dapper e connection factory Npgsql. |
| 10 | A API conecta no PostgreSQL? | Sim, via `PostgresConnectionFactory` e `DefaultConnection`; validação runtime depende de PostgreSQL local. |
| 11 | PostgreSQL sobe via Docker? | Sim, `docker-compose.postgres.yml` existe com PostgreSQL 16 na porta 5434. |
| 12 | `database/postgresql` possui migrations reais? | Sim. |
| 13 | As migrations são idempotentes? | Sim, usam `CREATE ... IF NOT EXISTS`, índices idempotentes e seeds com `ON CONFLICT`. |
| 14 | Existe seed dos cinco planos oficiais? | Sim, em `010_seed_official_plans.sql`. |
| 15 | Existe seed demo Valora Insight™? | Sim, em `011_seed_demo_valora_insight.sql`. |
| 16 | Existe endpoint `/health`? | Sim. |
| 17 | Existe endpoint `/health/database`? | Sim. |
| 18 | Existe endpoint `/plans/public`? | Sim. |
| 19 | Existe endpoint `/auth/register-company`? | Sim. |
| 20 | Existe endpoint `/auth/login`? | Sim. |
| 21 | Existe endpoint de validar pesquisa pública? | Sim: `POST /public/surveys/{surveyId}/validate`. |
| 22 | Existe endpoint de enviar resposta pública? | Sim: `POST /public/surveys/{surveyId}/responses`. |
| 23 | Existe endpoint de consultar resultado público? | Sim: `POST /public/results/{responseId}`. |
| 24 | Existe criação de certificate metadata? | Sim na jornada pública demo/API e no repository de certificados; geração binária completa ainda é inicial. |
| 25 | Existe criação de `email_job`? | Sim via repository/serviço; jornada demo retorna status de job e endpoint de comunicação registra intenção. |
| 26 | `migration/export-firestore.js` exporta dados reais? | Sim quando credenciais Firebase Admin estão disponíveis; também oferece `--dry-run`. |
| 27 | `migration/transform-firestore-to-postgres.js` transforma dados reais? | Sim, gera arquivos normalizados em `migration/out/`. |
| 28 | `migration/import-postgres.js` faz dry-run/apply/upsert? | Sim, suporta `--dry-run`, `--apply`, `--truncate`, `--batch-size` e `--backup`. |
| 29 | `migration/compare-firebase-postgres.js` gera relatório real? | Sim, gera `reports/migration-comparison.json` e atualiza `MIGRATION_COMPARISON_REPORT.md`; em dry-run usa amostras/arquivos locais. |
| 30 | `DATA_PROVIDER=api` funciona localmente? | Estrutura e validadores passam; execução ponta a ponta depende da API e PostgreSQL locais em execução. |
| 31 | `DATA_PROVIDER=hybrid` compara sem duplicar escrita? | Sim por contrato/validador: leitura usa primário e comparação secundária, escrita fica no primário. |
| 32 | O que ainda é mock, stub, demo-only, TODO ou NotImplemented? | Há rota 501 intencional no `communication-gateway` para endpoints legados, `SMTP_MOCK` de desenvolvimento, `migration/transform-to-postgres.js` legado com TODO, e fluxos demo da pesquisa pública para homologação local. |

## Mapeamento de termos exigidos

Comando executado:

```bash
rg -n "TODO|NotImplemented|not implemented|demo only|mock|stub|throw new Error|501|Valora.sln|Dapper|Npgsql|MigrationRunner|DATA_PROVIDER|HYBRID_PRIMARY_PROVIDER|ALLOW_API_PRODUCTION_CUTOVER|callPublicFunction|firebaseCallable|renderTakeSurvey|submitSurvey|renderResult|ValoraRepository|ValoraApiRepository" package.json config.js config/config.production.js runtime-capabilities.js index.html api-client.js api-repository.js gateway-client.js repository.js firebase-repository.js local-repository.js app.js pdf.js backend database/postgresql migration communication-gateway scripts tools/windows
```

Resultado: 157 ocorrências. Principais achados:

- `DATA_PROVIDER`, `HYBRID_PRIMARY_PROVIDER` e `ALLOW_API_PRODUCTION_CUTOVER` estão preservados em `config.js` e `config/config.production.js`, mantendo produção em Firebase e cutover bloqueado por padrão.
- `ValoraRepository`, `ValoraApiRepository`, `api-client.js`, `api-repository.js`, `gateway-client.js` e `runtime-capabilities.js` permanecem no frontend.
- `renderTakeSurvey`, `submitSurvey` e `renderResult` foram validados pelos scripts para não chamarem diretamente Cloud Functions na jornada pública principal.
- `Dapper`, `NpgsqlConnection` e `MigrationRunner` existem no backend.
- Ocorrências de `throw new Error` são majoritariamente validadores/scripts Node; não indicam endpoints obrigatórios com exceção proposital.
- Ocorrências de `501`/`not-implemented` ficam no gateway de comunicação legado para rotas não operacionais e não em endpoint obrigatório da API ASP.NET Core.
- O arquivo legado `migration/transform-to-postgres.js` ainda contém TODO e deve ser removido/substituído em sprint posterior somente após confirmar que não é mais referenciado.

## Diagnóstico real

O repositório já possuía uma base relevante da Sprint 10/11: solução ASP.NET Core, projetos em camadas, controllers, repositories Dapper, migrations PostgreSQL, provider híbrido no frontend, validadores e documentação. A Sprint 12 consolidou lacunas operacionais de checklist adicionando a migration obrigatória `007_certificate_tables.sql`, o script Windows consolidado `54-validar-api-postgres-migracao-real.bat` e esta auditoria objetiva.

## Riscos restantes

1. Build/testes .NET não puderam ser executados neste container por ausência do SDK `dotnet`.
2. Testes runtime com PostgreSQL/API dependem de Docker/PostgreSQL e da API ASP.NET Core em execução.
3. Certificado PDF é versão inicial textual; geração visual completa deve ficar para sprint dedicada.
4. Gateway de comunicação ainda possui rotas legadas 501 por design, enquanto o backend ASP.NET Core registra jobs/intenções.
5. Cutover de produção permanece bloqueado e deve continuar dependente de homologação, comparação de dados e rollback testado.
