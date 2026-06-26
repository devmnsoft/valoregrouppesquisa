# Testes executados — Sprint 6

- `npm run check`: sintaxe frontend/Firebase Functions validada.
- `node scripts/validate-api-provider.js`: provider API e ordem de scripts validados.
- `node scripts/validate-public-journey-provider.js`: jornada pública sem chamadas diretas a Cloud Functions validada.
- `node scripts/validate-architecture-warnings.js`: warnings críticos de arquitetura validados.

Validações Docker, PostgreSQL e backend completo devem ser executadas no ambiente Windows/IIS ou Linux com Docker e .NET SDK disponíveis.

## Complemento Sprint 8

## Estado Sprint 8
- Produção permanece segura com `DATA_PROVIDER: 'firebase'` e `ALLOW_API_PRODUCTION_CUTOVER: false`.
- API/PostgreSQL ficam disponíveis para homologação local/controlada com `DATA_PROVIDER: 'api'` ou `DATA_PROVIDER: 'hybrid'`.
- Firebase, `firebase-repository.js` e `repository.js` são preservados.
- Frontend não armazena SMTP, segredos de e-mail ou token WhatsApp; comunicação deve passar por Gateway/API.

- Validadores: `api:provider`, `journey:provider`, `architecture:warnings`, `cutover:ready`, `api:e2e`, `postgres:mvp`, `hybrid:check`, `migration:validate`, `migration:compare`.

## Sprint 11 — Backend/PostgreSQL operacional (2026-06-26)

- `npm run check`: PASS.
- `node scripts/validate-backend-implementation.js`: PASS.
- `node scripts/validate-api-provider.js`: PASS.
- `node scripts/validate-public-journey-provider.js`: PASS.
- `node scripts/validate-hybrid-provider.js`: PASS.
- `node scripts/validate-migration-scripts.js`: PASS.
- `node scripts/validate-real-migration-dry-run.js`: PASS.
- `node scripts/validate-cutover-readiness.js`: PASS.
- `node scripts/validate-release-candidate.js`: PASS.
- `npm run build:prod`: PASS.
- `node scripts/validate-architecture-warnings.js`: PASS.
- `node scripts/validate-postgres-schema.js`: PASS.
- `node scripts/validate-vertical-postgres-mvp.js`: PASS.
- `node migration/validate-migration.js --dry-run`: PASS.
- `node migration/compare-firebase-postgres.js --dry-run`: PASS, relatório atualizado.
- `node scripts/validate-api-backend.js`: PASS.
- `node scripts/validate-end-to-end-api-flow.js`: SKIP controlado porque `API_BASE_URL` não foi definido para fluxo HTTP completo.
- `dotnet build backend/Valora.sln`: não executado no container porque `dotnet` não está instalado (`command not found`).
- `dotnet test backend/Valora.sln`: não executado pelo mesmo motivo.
- `node scripts/healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa`: FAIL por indisponibilidade DNS/gateway externo e SMTP PRD não configurado no ambiente de validação; build local foi validado pelo próprio script.
