# Deep Audit v6.1

## Escopo auditado
A auditoria inicial obrigatória foi tentada com `dotnet clean`, `dotnet restore`, `dotnet build`, `dotnet test` e `dotnet format --verify-no-changes`, mas o ambiente não possui `dotnet` instalado. O repositório atual contém Valora/ValoraPesquisa e não uma árvore `src/HabitFlow.*`; portanto a v6.1 foi consolidada como pacote operacional de scripts, QA e documentação HabitFlow sem alterar código executável inexistente.

## Funcional
- Existe solução ASP.NET em `backend-v2/ValoraPesquisa.sln` com Web/API, Dapper/PostgreSQL e views administrativas ValoraPesquisa.
- O novo fluxo oficial `database/migrate.sql` referencia migrations 001 a 029 com caminhos consistentes.
- A migration 029 garante tabelas operacionais, constraint `SuperAdmin`, `schema_migrations`, índices e tabelas de billing/auditoria/comunicações.

## Parcial
- SuperAdmin HabitFlow real não existe na árvore atual; requer importação/criação dos projetos `HabitFlow.*`.
- Billing jobs reais foram especificados e suportados por schema, mas não há classes `BillingStatusJob`/`BillingCommunicationJob` HabitFlow para completar.
- Testes funcionais MVC HabitFlow não puderam ser adicionados porque o projeto e rotas não existem neste repo.

## Stub/inconsistente
- Seeds usam hashes marcados como `DEV_ONLY_REPLACE_WITH_HASH` e não devem ser usados em produção.
- `create-superadmin.ps1` prepara SQL seguro sem senha, mas depende do utilitário oficial de hash do projeto.

## Corrigido nesta versão
- Criado `database/migrate.sql` sincronizado 001-029.
- Criada `database/migrations/029_operational_completeness_v61.sql`.
- Criados `database/script_completo.sql`, `database/validate_schema_habitflow.sql`, `database/seed_dev.sql` e `database/seed_production_minimal.sql`.
- Criados scripts QA para banco, placeholders, Simple.cshtml, TODOs visíveis, links e assets.
- Criado script administrativo para primeiro SuperAdmin.
- Atualizados README, CHANGELOG e TODO técnico com riscos restantes.

## Riscos restantes
- Reexecutar todos os comandos dotnet em workstation/CI com SDK instalado.
- Integrar classes reais de serviços/repositories/controllers quando a árvore HabitFlow estiver presente.
- Rodar `psql` contra banco limpo e homologado.
- Validar manualmente fluxos SuperAdmin após implementação MVC.

## Checklist final
- [x] Migration 029 criada.
- [x] `migrate.sql` inclui 028 e 029.
- [x] Constraint de role inclui `User`, `Admin`, `SuperAdmin`.
- [x] `schema_migrations` preparado.
- [x] QA scripts criados.
- [ ] Build/test dotnet executado em ambiente com SDK.
- [ ] Jobs HabitFlow compilados e testados.
- [ ] Rotas SuperAdmin HabitFlow executadas em browser.
