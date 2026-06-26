# Schema PostgreSQL Valora

Sprint 4 mantém Firebase em produção e adiciona MVP operacional PostgreSQL/API para homologação controlada.

## Comandos principais

- Subir PostgreSQL: `npm run postgres:up`
- Build backend: `npm run backend:build`
- Testes backend: `npm run backend:test`
- Validar provider API: `npm run api:provider`
- Validar jornada pública: `npm run journey:provider`
- Dry-run de migração: `node migration/import-postgres.js --dry-run`
- Comparação: `npm run migration:compare`

## Segurança

- Produção permanece com `DATA_PROVIDER: 'firebase'`.
- Cloud Functions não são usadas pela jornada pública quando `ENABLE_CLOUD_FUNCTIONS` está falso.
- Senhas não são migradas em texto puro.

## Complemento Sprint 8

## Estado Sprint 8
- Produção permanece segura com `DATA_PROVIDER: 'firebase'` e `ALLOW_API_PRODUCTION_CUTOVER: false`.
- API/PostgreSQL ficam disponíveis para homologação local/controlada com `DATA_PROVIDER: 'api'` ou `DATA_PROVIDER: 'hybrid'`.
- Firebase, `firebase-repository.js` e `repository.js` são preservados.
- Frontend não armazena SMTP, segredos de e-mail ou token WhatsApp; comunicação deve passar por Gateway/API.

- Validadores: `api:provider`, `journey:provider`, `architecture:warnings`, `cutover:ready`, `api:e2e`, `postgres:mvp`, `hybrid:check`, `migration:validate`, `migration:compare`.
