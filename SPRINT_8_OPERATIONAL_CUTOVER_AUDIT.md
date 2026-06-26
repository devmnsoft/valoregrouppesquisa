# Sprint 8 — Auditoria Operacional de Cutover

## Diagnóstico real
1. `api-client.js`: existia e foi consolidado com timeout, AbortController, normalização de erro, sanitização e mensagem segura para HTML.
2. `api-repository.js`: existia e agora expõe saúde, banco, arquitetura, migração, certificados, e-mail e aliases.
3. `gateway-client.js`: permanece necessário temporariamente para e-mail e jornada pública enquanto API assume comunicação.
4. `index.html`: carrega config/runtime/firebase/local/api/gateway/repository antes de `app.js`; ordem preservada.
5. `runtime-capabilities.js`: detecta firebase/api/hybrid e agora expõe warnings e autorização explícita de cutover.
6. `repository.js`: é a porta oficial de provider, com firebase/api/hybrid e diagnóstico de divergência.
7. `app.js`: funções públicas usam wrappers centralizados `validatePublicSurveyLink`, `submitPublicSurveyResponse` e `loadPublicResult`, que delegam a `ValoraRepository`.
8. `renderTakeSurvey`: usa provider centralizado via validação pública.
9. `submitSurvey`: usa provider centralizado para envio público no modo Firebase/API/hybrid.
10. `renderResult`: usa provider centralizado para carregar resultado público.
11. `backend/Valora.sln`: existe.
12. Endpoints mínimos: controllers de health, admin, plans, auth, public, certificates e communications existem.
13. PostgreSQL: há `docker-compose.postgres.yml` e migrations.
14. Migrations: padrão idempotente com `CREATE ... IF NOT EXISTS` e seeds com conflito.
15. Seed de planos oficiais: `010_seed_official_plans.sql`.
16. Seed demo Valora Insight™: `011_seed_demo_valora_insight.sql`.
17. Migração: scripts export/transform/import/validate/compare existem.
18. Certificados: backend retorna endpoints seguros; geração completa pode evoluir sem HTML de erro.
19. E-mail: communication jobs ficam no backend/gateway, não no frontend.
20. Admin: status deve consumir runtime warnings, migração e healths.
21. Riscos para API em produção: migração não aplicada em PRD, comparação sem evidência final, gateway/SMTP pendente, certificados completos e janela de rollback.

## Ocorrências mapeadas
Foram mapeados `DATA_PROVIDER`, `HYBRID_PRIMARY_PROVIDER`, `API_BASE_URL`, providers públicos, `EMAIL_TRANSPORT`, `COMMUNICATION_GATEWAY`, `EXTERNAL_API_BASE_URL`, chamadas públicas, funções `renderTakeSurvey`/`submitSurvey`/`renderResult`, e objetos `ValoraApiRepository`, `ValoraGatewayClient` e `ValoraRepository` em `config*.js`, `runtime-capabilities.js`, `repository.js`, `api-*.js`, `gateway-client.js`, `app.js`, backend, scripts e documentação.

## Estado Sprint 8
- Produção permanece segura com `DATA_PROVIDER: 'firebase'` e `ALLOW_API_PRODUCTION_CUTOVER: false`.
- API/PostgreSQL ficam disponíveis para homologação local/controlada com `DATA_PROVIDER: 'api'` ou `DATA_PROVIDER: 'hybrid'`.
- Firebase, `firebase-repository.js` e `repository.js` são preservados.
- Frontend não armazena SMTP, segredos de e-mail ou token WhatsApp; comunicação deve passar por Gateway/API.
