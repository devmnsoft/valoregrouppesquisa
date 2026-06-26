# Sprint 11 — Auditoria Operacional Backend/PostgreSQL

Data: 2026-06-26

## Escopo auditado
Foram verificados `package.json`, configurações de runtime/produção, providers frontend, jornada pública, `backend/`, `database/postgresql/`, `migration/`, `communication-gateway/`, `scripts/` e `tools/windows/`.

## Respostas objetivas
1. `backend/Valora.sln` existe? **Sim**.
2. `backend/Valora.Api` existe? **Sim**.
3. `backend/Valora.Application` existe? **Sim**.
4. `backend/Valora.Domain` existe? **Sim**.
5. `backend/Valora.Infrastructure` existe? **Sim**.
6. `backend/Valora.Tests` existe? **Sim**.
7. `dotnet build backend/Valora.sln` passa? **A validar nesta sprint**.
8. A API possui controllers reais? **Sim**, há controllers para health, auth, public, plans, admin, communications e certificates.
9. A API possui repositories Dapper reais? **Parcial**, há Dapper/Npgsql, mas contratos obrigatórios precisavam ser ampliados.
10. A API conecta no PostgreSQL? **Sim**, via `PostgresConnectionFactory`.
11. `database/postgresql` existe? **Sim**.
12. As migrations são idempotentes? **Parcial**, os scripts usam `IF NOT EXISTS`/`ON CONFLICT`, mas havia duplicidades de numeração legada.
13. Existe schema migration/import/compare? **Sim**, mas precisava validação mais estrita.
14. Existe seed dos cinco planos? **Sim**.
15. Existe seed demo Valora Insight™? **Sim**.
16. Existe endpoint de health? **Sim**.
17. Existe endpoint de database health? **Sim**.
18. Existe endpoint de plans/public? **Sim**.
19. Existe endpoint de auth/register-company? **Sim**.
20. Existe endpoint de auth/login? **Sim**.
21. Existe endpoint de public survey validate? **Sim**.
22. Existe endpoint de public survey responses? **Sim**.
23. Existe endpoint de public results? **Sim**.
24. Existe email job? **Parcial**, há serviço/tabela; fluxo será reforçado.
25. Existe certificate metadata? **Parcial**, há serviço/tabela; fluxo será reforçado.
26. `migration/export-firestore.js` exporta dados reais? **Sim com credenciais Admin SDK ou dry-run controlado**.
27. `migration/transform-firestore-to-postgres.js` transforma dados reais? **Sim, a partir do export JSON**.
28. `migration/import-postgres.js` faz dry-run/apply/upsert? **Sim/Parcial**, opções existem e serão validadas.
29. `migration/compare-firebase-postgres.js` gera relatório real? **Sim/Parcial**, gera relatório local; comparação DB depende de ambiente.
30. `DATA_PROVIDER=api` funciona localmente? **Parcial**, provider existe; será validado.
31. `DATA_PROVIDER=hybrid` compara sem duplicar escrita? **Parcial**, provider existe; será validado/reforçado.
32. O que ainda é mock, stub, demo-only, TODO ou NotImplemented? Foram localizados marcadores em código, docs e dependências vendorizadas; endpoints obrigatórios não devem retornar `501`/`NotImplemented`.

## Mapeamento por termos obrigatórios
A busca operacional foi executada com `rg -n` para: `TODO`, `NotImplemented`, `not implemented`, `demo only`, `mock`, `stub`, `throw new Error`, `501`, `Valora.sln`, `Dapper`, `Npgsql`, `MigrationRunner`, `DATA_PROVIDER`, `HYBRID_PRIMARY_PROVIDER`, `ALLOW_API_PRODUCTION_CUTOVER`, `callPublicFunction`, `firebaseCallable`, `renderTakeSurvey`, `submitSurvey`, `renderResult`, `ValoraRepository`, `ValoraApiRepository`.

## Diagnóstico inicial
O repositório já continha uma base avançada da arquitetura híbrida e um backend ASP.NET Core. Os riscos principais eram: contratos/repositories obrigatórios incompletos, validações automatizadas tolerantes demais, scripts PostgreSQL com nomes duplicados legados, fluxo público backend simplificado, e documentação operacional ainda incompleta para cutover/rollback controlado.
