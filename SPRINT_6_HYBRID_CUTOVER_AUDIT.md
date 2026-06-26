# Sprint 6 — Auditoria da Arquitetura Híbrida e Cutover Firebase → PostgreSQL

Data: 2026-06-26.

## Diagnóstico real

O repositório já continha a ponte de arquitetura: `api-client.js`, `api-repository.js`, `gateway-client.js`, `runtime-capabilities.js`, `repository.js`, backend ASP.NET Core em `backend/Valora.sln`, migrations PostgreSQL em `database/postgresql/`, migração em `migration/`, validadores em `scripts/` e automações Windows em `tools/windows/`.

## Respostas obrigatórias

1. `api-client.js` está completo? Sim, agora usa `API_BASE_URL`, timeout via `API_TIMEOUT_MS`, `AbortController`, bearer token, erro para HTML e JSON inválido, e expõe limpar/recuperar token.
2. `api-repository.js` está completo? Sim, agora expõe login, cadastro, `/me`, planos, jornada pública, certificados, migração, health da API e health do banco, com aliases compatíveis.
3. `index.html` carrega os arquivos na ordem correta? Sim: `api-client.js` antes de `api-repository.js`, `gateway-client.js` antes de `repository.js` e `app.js` por último.
4. `runtime-capabilities.js` detecta `DATA_PROVIDER` corretamente? Sim, normaliza `firebase`, `api` e `hybrid` e expõe `dataProvider`.
5. `DATA_PROVIDER=api` é usado de verdade no `app.js`? Sim, a jornada pública chama `window.ValoraRepository`, que roteia para `ValoraApiRepository` quando configurado.
6. `DATA_PROVIDER=hybrid` compara dados sem duplicar escrita? Sim, `repository.js` compara leituras em segundo plano e escreve somente no provider primário.
7. `renderTakeSurvey` ainda chama Cloud Functions diretamente? Não.
8. `submitSurvey` ainda chama Cloud Functions diretamente? Não.
9. `renderResult` ainda depende de Cloud Functions? Não para a jornada pública; usa `loadPublicResult` via provider único.
10. `backend/Valora.sln` existe e compila? Existe; validação registrada em `TESTES_EXECUTADOS.md`.
11. PostgreSQL sobe via Docker? Existe `docker-compose.postgres.yml`; validação depende de Docker local.
12. Migrations SQL existem e são idempotentes? Sim, há scripts `001` a `011` com `IF NOT EXISTS` e `ON CONFLICT`.
13. Seed dos planos oficiais existe? Sim, `010_seed_official_plans.sql`.
14. Seed demo Valora Insight™ existe? Sim, `011_seed_demo_valora_insight.sql`.
15. Scripts de migração Firestore → PostgreSQL existem? Sim.
16. Migração tem dry-run, apply e comparação? Sim, `import-postgres.js` suporta dry-run/apply e `compare-firebase-postgres.js` gera relatório.
17. Bloqueios para ativar `DATA_PROVIDER=api` em produção: homologação ponta a ponta em Windows/IIS, carga real migrada e comparada, SMTP transacional real, certificados finais e janela de rollback aprovada.

## Ocorrências mapeadas

Foram mapeados `DATA_PROVIDER`, `HYBRID_PRIMARY_PROVIDER`, `API_BASE_URL`, providers públicos, `callPublicFunction`, `firebaseCallable`, `submitSurveyResponse`, `validateSurveyLink`, `renderTakeSurvey`, `submitSurvey`, `renderResult`, `loadPublicResult`, `ValoraApiRepository` e `ValoraGatewayClient` em `config.js`, `runtime-capabilities.js`, `repository.js`, `api-repository.js`, `gateway-client.js`, `app.js`, validadores, documentação e scripts Windows.

## Estado final da sprint

Produção permanece segura em `DATA_PROVIDER: 'firebase'`. A API/PostgreSQL fica disponível para homologação local/controlada com `DATA_PROVIDER=api` ou `DATA_PROVIDER=hybrid`, sem cutover automático.
