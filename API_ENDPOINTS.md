# Endpoints API

- `GET /health`, `/health/database`, `/health/config`
- `GET /plans/public`
- `POST /auth/register-company`, `/auth/login`, `/auth/forgot-password`, `GET /me`
- `POST /public/surveys/{surveyId}/validate`, `/responses`, `/public/results/{responseId}`
- `GET /responses/{responseId}/certificate.pdf`, `.png`
- `POST /communications/result/{responseId}/send-email`, `GET /communications`
- `GET /admin/migration/status`

## Complemento Sprint 8

## Estado Sprint 8
- Produção permanece segura com `DATA_PROVIDER: 'firebase'` e `ALLOW_API_PRODUCTION_CUTOVER: false`.
- API/PostgreSQL ficam disponíveis para homologação local/controlada com `DATA_PROVIDER: 'api'` ou `DATA_PROVIDER: 'hybrid'`.
- Firebase, `firebase-repository.js` e `repository.js` são preservados.
- Frontend não armazena SMTP, segredos de e-mail ou token WhatsApp; comunicação deve passar por Gateway/API.

- Validadores: `api:provider`, `journey:provider`, `architecture:warnings`, `cutover:ready`, `api:e2e`, `postgres:mvp`, `hybrid:check`, `migration:validate`, `migration:compare`.
