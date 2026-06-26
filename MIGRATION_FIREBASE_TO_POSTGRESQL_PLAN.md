# Plano de Migração Firebase → PostgreSQL

1. Exportar Firestore com `node migration/export-firestore.js`.
2. Transformar com `node migration/transform-firestore-to-postgres.js`.
3. Validar dry-run com `node migration/import-postgres.js --dry-run`.
4. Aplicar localmente com `--apply`.
5. Comparar com `node migration/compare-firebase-postgres.js`.

## Complemento Sprint 8

## Estado Sprint 8
- Produção permanece segura com `DATA_PROVIDER: 'firebase'` e `ALLOW_API_PRODUCTION_CUTOVER: false`.
- API/PostgreSQL ficam disponíveis para homologação local/controlada com `DATA_PROVIDER: 'api'` ou `DATA_PROVIDER: 'hybrid'`.
- Firebase, `firebase-repository.js` e `repository.js` são preservados.
- Frontend não armazena SMTP, segredos de e-mail ou token WhatsApp; comunicação deve passar por Gateway/API.

- Validadores: `api:provider`, `journey:provider`, `architecture:warnings`, `cutover:ready`, `api:e2e`, `postgres:mvp`, `hybrid:check`, `migration:validate`, `migration:compare`.
