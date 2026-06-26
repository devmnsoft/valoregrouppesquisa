# Sprint 9 — Auditoria real de ensaio de produção API/PostgreSQL

Data: 2026-06-26.

## Escopo auditado

Arquivos/pastas: `package.json`, `config.js`, `runtime-capabilities.js`, `index.html`, `api-client.js`, `api-repository.js`, `gateway-client.js`, `repository.js`, `firebase-repository.js`, `local-repository.js`, `app.js`, `pdf.js`, `backend/`, `database/postgresql/`, `migration/`, `communication-gateway/`, `scripts/`, `tools/windows/`.

## Respostas objetivas

1. `api-client.js` já estava funcional, mas precisava alinhar mensagens/validações com os validadores da Sprint 9.
2. `api-repository.js` cobre os métodos principais usados pelo `app.js` e agora mantém aliases para evitar quebra por nomes divergentes.
3. `gateway-client.js` ainda é necessário como ponte de comunicação/compatibilidade enquanto e-mail e jornadas públicas podem usar gateway externo.
4. `index.html` carrega `api-client.js` antes de `api-repository.js` e ambos antes de `repository.js`.
5. `runtime-capabilities.js` detecta `firebase`, `api` e `hybrid`, e agora expõe alertas estruturados de cutover/arquitetura.
6. `repository.js` é a camada central de provider para Firebase/API/Hybrid e registra diagnósticos de comparação.
7. `app.js` mantém wrappers legados de Cloud Functions, mas a rota pública usa `ValoraRepository` através de `validatePublicSurveyLink`, `submitPublicSurveyResponse` e `loadPublicResult`.
8. `renderTakeSurvey` usa `ValoraRepository` indiretamente por `validatePublicSurveyLink`.
9. `submitSurvey` usa `ValoraRepository` indiretamente por `submitPublicSurveyResponse`.
10. `renderResult` usa `ValoraRepository` indiretamente por `loadPublicResult`.
11. `backend/Valora.sln` existe.
12. Backend compilável por `dotnet build backend/Valora.sln` quando o SDK .NET está disponível.
13. Endpoints mínimos existem nos controllers do backend.
14. PostgreSQL possui `docker-compose.postgres.yml` para subida local.
15. Migrations usam padrão idempotente (`CREATE ... IF NOT EXISTS`, `ON CONFLICT`) e wrappers foram alinhados para a numeração esperada.
16. Seed dos planos oficiais existe em `010_seed_official_plans.sql`.
17. Seed demo Valora Insight™ existe em `011_seed_demo_valora_insight.sql`.
18. `migration/export-firestore.js` existe; foi completado para exportar coleções reais quando credenciais estiverem disponíveis e para executar dry-run seguro.
19. `migration/transform-firestore-to-postgres.js` existe; foi completado para gerar arquivos normalizados em `migration/out`.
20. `migration/import-postgres.js` existe; foi completado com flags `--dry-run`, `--apply`, `--truncate`, `--batch-size` e `--backup`.
21. `migration/compare-firebase-postgres.js` existe; foi completado para gerar `reports/migration-comparison.json` e `MIGRATION_COMPARISON_REPORT.md`.
22. Admin mostra status de arquitetura/migração via runtime, repository e diagnósticos hybrid.
23. Bloqueios de cutover: aprovação explícita, backup Firebase/PostgreSQL, dry-run, apply local, comparação sem divergências críticas, saúde API/PostgreSQL, SMTP/certificados e janela de rollback.

## Mapeamento de ocorrências

Executado `rg -n` para: `DATA_PROVIDER`, `HYBRID_PRIMARY_PROVIDER`, `ALLOW_API_PRODUCTION_CUTOVER`, `API_BASE_URL`, `PUBLIC_SURVEY_VALIDATION_PROVIDER`, `PUBLIC_SUBMISSION_PROVIDER`, `RESULT_PROVIDER`, `EMAIL_TRANSPORT`, `COMMUNICATION_GATEWAY`, `EXTERNAL_API_BASE_URL`, `callPublicFunction`, `firebaseCallable`, `submitSurveyResponse`, `validateSurveyLink`, `renderTakeSurvey`, `submitSurvey`, `renderResult`, `loadPublicResult`, `ValoraApiRepository`, `ValoraGatewayClient`, `ValoraRepository`.

Principais pontos: configurações em `config.js`/`config/`; runtime em `runtime-capabilities.js`; provider central em `repository.js`; jornada pública em `app.js`; API frontend em `api-client.js` e `api-repository.js`; gateway em `gateway-client.js`; validações em `scripts/`; documentação de cutover/rollback em Markdown.

## Diagnóstico final

Produção permanece com `DATA_PROVIDER: 'firebase'` e `ALLOW_API_PRODUCTION_CUTOVER: false`. API e Hybrid estão restritos a homologação local/controlada. Cutover para PostgreSQL/API continua bloqueado até conclusão da checklist operacional e autorização explícita.
