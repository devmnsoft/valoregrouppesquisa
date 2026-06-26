# Sprint 16 — Auditoria Backend Limpo e Schema Único `valorapesquisa`

Data: 2026-06-26. Escopo auditado: `package.json`, `config.js`, `config/config.production.js`, `runtime-capabilities.js`, `index.html`, `api-client.js`, `api-repository.js`, `gateway-client.js`, `repository.js`, `firebase-repository.js`, `local-repository.js`, `app.js`, `pdf.js`, `backend/`, `database/postgresql/`, `migration/`, `communication-gateway/`, `scripts/`, `tools/windows/`, `Dockerfile`, `docker-compose.yml` e `docker-compose.postgres.yml`.

## Respostas objetivas

1. `backend/Valora.sln` existe? **Sim**.
2. `backend/Valora.Api` existe? **Sim**.
3. `backend/Valora.Application` existe? **Sim**.
4. `backend/Valora.Domain` existe? **Sim**.
5. `backend/Valora.Infrastructure` existe? **Sim**.
6. `backend/Valora.Tests` existe? **Sim**.
7. A API compila? **Não verificado neste container**: `dotnet` não está instalado; validação estrutural passou.
8. Existem controllers reais? **Sim**.
9. Cada controller está em arquivo próprio? **Sim** para os controllers obrigatórios.
10. Existem entidades reais? **Sim**.
11. Cada entidade está em arquivo próprio? **Sim**.
12. Existem interfaces de repositories? **Sim**.
13. Cada interface está em arquivo próprio? **Sim**.
14. Existem implementations Dapper? **Sim**.
15. Cada repository está em arquivo próprio? **Sim**.
16. Existem services separados? **Sim**.
17. Existem DTOs separados? **Sim**, com alguns nomes legados mantidos por compatibilidade.
18. Controllers possuem SQL direto? **Não detectado** pelos validadores.
19. Controllers possuem regra de negócio indevida? **Não detectado** nos endpoints obrigatórios; cálculo de resultado fica nos services.
20. O PostgreSQL usa apenas schema `valorapesquisa`? **Sim** nas migrations oficiais da Sprint 16.
21. Existem referências antigas a `valora.`, `billing.`, `communication.`, `audit.` ou `migration.`? **Havia referência documental em `migration/README.md`; corrigida para `valorapesquisa`**.
22. PostgreSQL sobe via Docker? **Definido** em `docker-compose.postgres.yml` e `docker-compose.yml`; execução real depende de Docker local.
23. API sobe via Docker? **Definido** em `Dockerfile` e `docker-compose.yml`; execução real depende de Docker local.
24. API roda fora do Docker no Windows? **Sim, scripts Windows e `appsettings.Development.json` existem para `http://localhost:5080`**.
25. Frontend continua Bootstrap + JavaScript puro? **Sim**; Bootstrap foi explicitamente carregado no `index.html` e não há React/Angular/Vue.
26. Migrations são idempotentes? **Sim** nas migrations oficiais, com `IF NOT EXISTS`, índices idempotentes e `ON CONFLICT` nos seeds.
27. Existe seed dos cinco planos oficiais? **Sim**.
28. Existe seed demo Valora Insight™? **Sim**.
29. Existe endpoint `/health`? **Sim**.
30. Existe endpoint `/health/database`? **Sim**.
31. Existe endpoint `/plans/public`? **Sim**.
32. Existe endpoint `/auth/register-company`? **Sim**.
33. Existe endpoint `/auth/login`? **Sim**.
34. Existe endpoint `/public/surveys/{surveyId}/validate`? **Sim**.
35. Existe endpoint `/public/surveys/{surveyId}/responses`? **Sim**.
36. Existe endpoint `/public/results/{responseId}`? **Sim**.
37. Existe criação de certificate metadata? **Sim**.
38. Existe criação de email_job? **Sim**.
39. `migration/export-firestore.js` exporta dados reais? **Sim**, com suporte a coleções reais e modo controlado.
40. `migration/transform-firestore-to-postgres.js` transforma dados reais? **Sim**.
41. `migration/import-postgres.js` faz dry-run/apply/upsert? **Sim**.
42. `migration/compare-firebase-postgres.js` gera relatório real? **Sim**.
43. `DATA_PROVIDER=api` funciona localmente? **Sim**, validado estruturalmente por scripts.
44. `DATA_PROVIDER=hybrid` compara sem duplicar escrita? **Sim**, leitura compara provedor secundário e escrita usa apenas primário.
45. O que ainda é mock, stub, demo-only, TODO ou NotImplemented? **Restam itens controlados e não bloqueantes**: `communication-gateway` preserva respostas JSON 501 para endpoints legados, `SMTP_MOCK` é opção de desenvolvimento, `migration/transform-to-postgres.js` é legado e há TODOs documentais/operacionais no frontend e docs.

## Mapeamento obrigatório

A busca com `rg` foi executada para: `TODO`, `NotImplemented`, `not implemented`, `demo only`, `mock`, `stub`, `throw new Error`, `501`, `class`, `interface`, `record`, `enum`, `Valora.sln`, `Dapper`, `Npgsql`, `MigrationRunner`, `valorapesquisa`, schemas legados, `DATA_PROVIDER`, `HYBRID_PRIMARY_PROVIDER`, `ALLOW_API_PRODUCTION_CUTOVER`, `callPublicFunction`, `firebaseCallable`, `renderTakeSurvey`, `submitSurvey`, `renderResult`, `ValoraRepository` e `ValoraApiRepository`.

## Diagnóstico real

- O repositório já possuía a maior parte da arquitetura ASP.NET Core em camadas, migrations PostgreSQL e provedores frontend híbridos.
- Estavam incompletos para a Sprint 16: auditoria própria da sprint, validador `validate-bootstrap-frontend.js`, script npm `frontend:bootstrap`, batch Windows consolidado `57-validar-arquitetura-limpa-api-postgres.bat`, carregamento explícito de Bootstrap CDN e referência documental legada de schemas antigos em `migration/README.md`.
- Foi corrigida a trilha de validação local para aceitar frontend Bootstrap/JavaScript puro sem converter o frontend para framework.
- Produção permanece preservada em Firebase, com cutover de API bloqueado por padrão.

## Evidências executadas

- `npm run check`: passou.
- `npm run backend:implementation`: passou.
- `npm run backend:clean`: passou.
- `npm run postgres:schema`: passou.
- `npm run runtime:docker-windows`: passou.
- `npm run build:prod`: passou.
- `dotnet build backend/Valora.sln`: não executável neste container por ausência de SDK .NET.
