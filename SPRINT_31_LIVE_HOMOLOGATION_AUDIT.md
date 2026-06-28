# Sprint 31 — Live Homologation Audit

## Respostas objetivas
1. Gates que ainda passavam sem API: `prod:saas-e2e`, `prod:cutover-dry-run`, `prod:rollback-ready`, checks estáticos de frontend/backend.
2. Sem PostgreSQL: os mesmos gates documentais e os validadores por string.
3. Sem migrations: gates que não chamavam `/health/migration`.
4. Só busca por string: coverage SaaS, segurança estática, rollback/cutover antigos.
5. Não simulavam usuário real: release final, frontend coverage, security gate estático.
6. Fluxos sem E2E vivo: cadastro/login/me, submissão pública com 25 respostas, certificado, e-mail, audit, usage.
7. Telas sem navegador real: login, cadastro, pesquisa pública, resultado/certificado, dashboard e mobile.
8. Endpoints não chamados: `/auth/register-company`, `/auth/login`, `/me`, `/public/*`, certificados e admin probes.
9. Módulos sem banco real: e-mail/audit/usage em gates de produção antigos.
10. Scripts que não falhavam com API offline: SaaS E2E antigo, cutover e rollback documentais.
11. Scripts que não falhavam com PostgreSQL offline: checks estáticos e contrato.
12. Fluxos sem rollback: submissão pública, import e provider switch.
13. Cutover dry-run real ausente: export/transform/import/compare/rollback local era documental.
14. Loading infinito: erros de API em login, pesquisa pública e resultado exigem browser E2E.
15. Mobile vulnerável: menu, pesquisa pública, certificado e dashboard.
16. Funcionalidades que podiam ir sem live: SaaS core, certificado, email job, audit, usage e limites.
17. Riscos finais: fixture live, rate limit local-prod, cutover Firestore real e execução Windows.

## Mapeamento de marcadores
- `VALORA_E2E_LIVE`: substituído por `--live` obrigatório no gate final.
- `not set`: não é mais caminho de aprovação em `prod:saas-e2e`.
- `validated executable live-check contract`: isolado em `prod:saas-e2e:contract`.
- `without calling local API`: proibido no gate final.
- `console.log`, `PASS`, `mock`, `stub`, `pending`, `demo`, `hardcoded`, `TODO`, `NotImplemented`, `NotSupportedException`, `/legacy/`, `fetch(`, `axios`, `http://localhost`, `playwright`, `chromium`: devem ser revisados nos gates estáticos e browser E2E.
