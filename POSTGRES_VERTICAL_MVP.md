# MVP Vertical PostgreSQL

Entregue como arquitetura paralela: Docker PostgreSQL 16 na porta 5434, API ASP.NET Core em `backend/Valora.sln`, seed dos planos oficiais e demo Valora Insight™. Produção permanece em Firebase via `DATA_PROVIDER: 'firebase'`.

## Execução
1. `npm run postgres:up`
2. `dotnet run --project backend/Valora.Api`
3. `npm run api:provider && npm run journey:provider`

## Complemento Sprint 8

## Estado Sprint 8
- Produção permanece segura com `DATA_PROVIDER: 'firebase'` e `ALLOW_API_PRODUCTION_CUTOVER: false`.
- API/PostgreSQL ficam disponíveis para homologação local/controlada com `DATA_PROVIDER: 'api'` ou `DATA_PROVIDER: 'hybrid'`.
- Firebase, `firebase-repository.js` e `repository.js` são preservados.
- Frontend não armazena SMTP, segredos de e-mail ou token WhatsApp; comunicação deve passar por Gateway/API.

- Validadores: `api:provider`, `journey:provider`, `architecture:warnings`, `cutover:ready`, `api:e2e`, `postgres:mvp`, `hybrid:check`, `migration:validate`, `migration:compare`.
