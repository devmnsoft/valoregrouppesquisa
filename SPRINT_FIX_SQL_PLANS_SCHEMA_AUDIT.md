# Sprint Fix SQL Plans Schema Audit

## 1. Resumo

A correção removeu o contrato antigo de planos dos scripts SQL oficiais e consolidou o schema usando `plans.code`, preços numéricos, limites estruturados, capabilities booleanas e vínculo de organização por `plan_code`/`subscriptions`.

## 2. Causa do erro

O seed oficial tentava inserir `price_label` em `valorapesquisa.plans`, mas o contrato oficial atual não deve depender dessa coluna. Além disso, o seed usava `plans.id` como código textual, o que conflita com o modelo UUID + `code`.

## 3. Arquivos corrigidos

- `scriptbd_completo.sql`
- `database/postgresql/scriptbd_completo.sql`
- `database/postgresql/002_core_tables.sql`
- `database/postgresql/003_plan_tables.sql`
- `database/postgresql/011_seed_official_plans.sql`
- `database/postgresql/012_seed_demo_valora_insight.sql`
- `database/postgresql/099_seed_e2e_live_fixture.sql`
- `backend/Valora.Infrastructure/Repositories/PlanRepository.cs`
- `backend/Valora.Infrastructure/Repositories/OrganizationRepository.cs`
- `backend/Valora.Tests/AdminRepositoryMigrationTests.cs`
- `backend/Valora.Tests/OfficialSqlPlansSchemaTests.cs`
- `tools/validate-backend-official-sql-schema.js`
- `package.json`

## 4. Schema real identificado

- `plans`: `id uuid`, `code text unique`, `monthly_price`, `annual_price`, metadados públicos, status e timestamps.
- `plan_limits`: uma linha por plano com colunas diretas de limites.
- `plan_capabilities`: uma linha por plano/capability com `capability_code` e `enabled`.
- `organizations`: vínculo textual em `plan_code`.
- `subscriptions`: vínculo relacional por `plan_id uuid` obtido de `plans.id`.

## 5. INSERTs corrigidos

O seed de planos foi refeito para usar `INSERT INTO valorapesquisa.plans(code, ...)` e `ON CONFLICT (code) DO UPDATE`.

## 6. Plan limits corrigidos

`plan_limits(plan_id, limit_key, limit_value)` foi substituído por insert estruturado com lookup `plans.code -> plans.id`.

## 7. Plan capabilities corrigidos

`capability_key`, `capability_level` e `capability_type` foram substituídos por `capability_code` e `enabled`.

## 8. Organização/assinatura corrigida

A organização Valora é criada/atualizada com `plan_code='enterprise'`. A assinatura é criada/atualizada em `subscriptions` usando o UUID do plano `enterprise`.

## 9. Validadores criados/atualizados

Criado `tools/validate-backend-official-sql-schema.js` e registrado `npm run backend:sql-schema-validate` no `package.json`.

## 10. Testes criados/atualizados

- Atualizado teste estático de schema em `AdminRepositoryMigrationTests`.
- Criado `OfficialSqlPlansSchemaTests` para bloquear regressões de `price_label`, seeds por `id`, limits/capabilities antigos e organização com `plan_id` inexistente.

## 11. Comandos executados

- `npm run backend:official-validate` — PASS.
- `npm run backend:reports-email-validate` — PASS.
- `npm run backend:migration-import-validate` — PASS.
- `npm run backend:homologation-cutover-validate` — PASS.
- `npm run backend:sql-schema-validate` — PASS.
- `npm run check:critical` — PASS.
- `dotnet --version && dotnet build backend/Valora.sln && dotnet test backend/Valora.sln` — não executou build/test porque o binário `dotnet` não existe no ambiente.

## 12. Comandos não executados e motivo

- `dotnet build backend/Valora.sln` — não executado porque `dotnet --version` falhou com `command not found`.
- `dotnet test backend/Valora.sln` — não executado pelo mesmo motivo.

## 13. Gaps restantes

- Não foi criada migração destrutiva/transformacional para bancos legados já materializados com `plans.id text`.
- Validadores são estáticos; não substituem execução real contra PostgreSQL de homologação.

## 14. Riscos

- Ambientes com schema antigo precisarão de plano de migração de dados antes de aplicar o contrato UUID + `code`.
- Integrações externas precisam deixar de usar `price_label` e IDs textuais em FKs.

## 15. Próximo passo recomendado

Executar o script completo em uma base PostgreSQL descartável de homologação e validar o fluxo de criação/consulta de planos e assinaturas ponta a ponta.
