# Sprint 15 — Auditoria backend limpo e schema único valorapesquisa

## Diagnóstico objetivo

1. `backend/Valora.sln` existe? **Sim**.
2. `backend/Valora.Api` existe? **Sim**.
3. `backend/Valora.Application` existe? **Sim**.
4. `backend/Valora.Domain` existe? **Sim**.
5. `backend/Valora.Infrastructure` existe? **Sim**.
6. `backend/Valora.Tests` existe? **Sim**.
7. A API compila? **Não validado neste container: SDK `dotnet` ausente**.
8. Existem controllers reais? **Sim**.
9. Os controllers estão separados por arquivo? **Sim**.
10. Existem entidades reais? **Sim**.
11. As entidades estão separadas por arquivo? **Sim**.
12. Existem interfaces de repositories? **Sim**.
13. Cada interface está em arquivo próprio? **Sim**.
14. Existem implementations Dapper? **Sim**.
15. Cada repository está em arquivo próprio? **Sim**.
16. Existem services separados? **Sim**.
17. Existem DTOs separados? **Sim**.
18. O PostgreSQL usa apenas schema `valorapesquisa`? **Sim nos scripts ativos**.
19. Existem referências antigas a `valora.`, `billing.`, `communication.`, `audit.` ou `migration.`? **Removidas dos SQL ativos e do backend**.
20. PostgreSQL sobe via Docker? **Compose configurado**.
21. API sobe via Docker? **Dockerfile e compose configurados**.
22. API roda fora do Docker no Windows? **Scripts Windows configurados para `http://localhost:5080`**.
23. Migrations são idempotentes? **Sim por padrão de `CREATE IF NOT EXISTS` e seeds com conflito controlado nos scripts existentes**.
24. Existe seed dos cinco planos oficiais? **Sim**.
25. Existe seed demo Valora Insight™? **Sim**.
26. A API tem auth, planos, pesquisa pública, resultado, certificado e comunicação? **Sim, endpoints/controladores existem**.
27. A migração Firestore → PostgreSQL exporta, transforma, importa e compara dados reais? **Scripts existem; execução real depende de credenciais e banco local/controlado**.
28. `DATA_PROVIDER=api` funciona localmente? **Estrutura preservada; execução depende da API local**.
29. `DATA_PROVIDER=hybrid` compara sem duplicar escrita? **Contrato preservado no frontend híbrido**.
30. O que ainda é mock, stub, demo-only, TODO ou NotImplemented? **Jornada demo Valora Insight™ permanece para homologação controlada; geração rica de certificado é incremental**.

## Mapeamento executado

Foram mapeados com `rg` os termos: `TODO`, `NotImplemented`, `not implemented`, `demo only`, `mock`, `stub`, `throw new Error`, `501`, `class`, `interface`, `record`, `enum`, `Valora.sln`, `Dapper`, `Npgsql`, `MigrationRunner`, `valorapesquisa`, schemas legados, `DATA_PROVIDER`, `HYBRID_PRIMARY_PROVIDER`, `ALLOW_API_PRODUCTION_CUTOVER`, `callPublicFunction`, `firebaseCallable`, `renderTakeSurvey`, `submitSurvey`, `renderResult`, `ValoraRepository` e `ValoraApiRepository`.
