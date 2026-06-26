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
