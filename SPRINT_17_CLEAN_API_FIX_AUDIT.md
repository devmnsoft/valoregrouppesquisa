# Sprint 17 — Auditoria da correção Clean API/PostgreSQL

## Diagnóstico objetivo

1. `backend/Valora.sln` existe: sim.
2. Projetos backend existem e permanecem na solução: Api, Application, Domain, Infrastructure e Tests.
3. Backend: não foi possível compilar neste contêiner porque `dotnet` não está instalado; o build deve ser executado em Windows/Docker com SDK .NET 8.
4. Controllers vazios encontrados antes da correção: `PublicSurveysController`, `PublicResultsController`, `ArchitectureController`, `AdminDatabaseController`, `MigrationController`.
5. Controllers compactados em uma linha encontrados: `AuthController`, `PlansController`, `OrganizationsController`, `SurveysController` e parte do `Program.cs`.
6. Rotas obrigatórias ausentes antes da correção nos controllers oficiais públicos e administrativos.
7. Regra de negócio indevida: cálculo demo e jornada pública estavam concentrados em `PublicController`; os endpoints oficiais foram movidos para controllers dedicados.
8. SQL direto em controllers: não identificado nos controllers alterados.
9. Services faltantes/DI incompleta: registros de Application estavam concentrados no `Program.cs`.
10. Repositories faltantes: `ResponseRepository` não existia e `IResponseRepository` apontava para `SurveyRepository`.
11. Interfaces faltantes: contratos principais já existiam, mas DI usava namespaces/registros concentrados.
12. Entidades faltantes: entidades principais já existem em arquivos separados.
13. DTOs faltantes: DTOs públicos e de auth já existem em arquivos separados.
14. Schema `valora` legado: encontrado no `MigrationRunner` e em migrations não canônicas; corrigido no runner e migrations não canônicas foram removidas da pasta executável.
15. Schema `billing`: migrations não canônicas arquivadas fora da pasta executável.
16. Schema `communication`: migrations não canônicas arquivadas fora da pasta executável.
17. Schema `audit`: migrations não canônicas arquivadas fora da pasta executável.
18. Schema `migration`: migrations não canônicas arquivadas fora da pasta executável.
19. `MigrationRunner`: corrigido para usar somente `valorapesquisa.schema_migrations`.
20. Scripts SQL executáveis: somente migrations canônicas `001` a `012` em `database/postgresql`.
21. Scripts de migração JS: permanecem para dry-run/export/transform/import/compare e serão validados por scripts dedicados.
22. Backend: repositories usam `valorapesquisa.nome_da_tabela`.
23. Frontend: preservado em Bootstrap + JavaScript puro, com Firebase e provider híbrido mantidos.
24. Docker: compose existente preservado para API/PostgreSQL.
25. Windows fora do Docker: scripts backend existentes preservados e script 58 criado.
26. Endpoints públicos oficiais: implementados em `PublicSurveysController` e `PublicResultsController`.
27. `DATA_PROVIDER=api`: caminho local preservado; validação via scripts.
28. `DATA_PROVIDER=hybrid`: caminho comparativo preservado; sem troca automática de produção.
29. Pendências: geração real de PDF/PNG, SMTP real e validação com banco real dependem de ambiente runtime.

## Ocorrências mapeadas

Foram revisados termos `TODO`, `NotImplemented`, `demo only`, `mock`, `stub`, `throw new Error`, `501`, schemas legados, `valorapesquisa.`, `callPublicFunction`, `firebaseCallable`, `renderTakeSurvey`, `submitSurvey` e `renderResult`. A correção priorizou blockers de arquitetura limpa, DI, rotas oficiais e schema único PostgreSQL executável.
