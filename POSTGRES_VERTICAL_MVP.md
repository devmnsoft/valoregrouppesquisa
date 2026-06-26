# MVP Vertical PostgreSQL

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
