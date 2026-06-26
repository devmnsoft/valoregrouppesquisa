# Arquitetura Alvo Valora API/PostgreSQL

A arquitetura alvo mantém Firebase como produção atual e adiciona ASP.NET Core + PostgreSQL em paralelo. O frontend alterna por `DATA_PROVIDER=firebase|api|hybrid`; modo híbrido lê/escreve no primário e compara secundário sem duplicar escrita.

## Complemento Sprint 8

## Estado Sprint 8
- Produção permanece segura com `DATA_PROVIDER: 'firebase'` e `ALLOW_API_PRODUCTION_CUTOVER: false`.
- API/PostgreSQL ficam disponíveis para homologação local/controlada com `DATA_PROVIDER: 'api'` ou `DATA_PROVIDER: 'hybrid'`.
- Firebase, `firebase-repository.js` e `repository.js` são preservados.
- Frontend não armazena SMTP, segredos de e-mail ou token WhatsApp; comunicação deve passar por Gateway/API.

- Validadores: `api:provider`, `journey:provider`, `architecture:warnings`, `cutover:ready`, `api:e2e`, `postgres:mvp`, `hybrid:check`, `migration:validate`, `migration:compare`.
