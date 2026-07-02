# Sprint Fix SQL Plans Schema Diagnostic

## 1. Onde `price_label` aparece

Antes da correção, `price_label` aparecia no DDL e nos seeds oficiais de planos em:

- `scriptbd_completo.sql`
- `database/postgresql/scriptbd_completo.sql`
- `database/postgresql/003_plan_tables.sql`
- `database/postgresql/011_seed_official_plans.sql`
- `database/postgresql/099_seed_e2e_live_fixture.sql`
- consultas do backend em `backend/Valora.Infrastructure/Repositories/PlanRepository.cs`
- testes estáticos que validavam o contrato antigo em `backend/Valora.Tests/AdminRepositoryMigrationTests.cs`

Após a correção, os scripts SQL oficiais não usam `price_label`.

## 2. Onde `plans(id, name, description, price_label...)` aparece

O padrão antigo foi identificado nos blocos de seed dos scripts completos:

- `scriptbd_completo.sql`
- `database/postgresql/scriptbd_completo.sql`

O padrão mais completo `plans(id,name,badge,...,price_label,...)` também existia em:

- `database/postgresql/011_seed_official_plans.sql`
- `database/postgresql/099_seed_e2e_live_fixture.sql`

Todos foram substituídos por `plans(code, ..., monthly_price, annual_price, ...)`.

## 3. Schema real de `valorapesquisa.plans`

Contrato oficial consolidado nesta correção:

- `id uuid PRIMARY KEY DEFAULT gen_random_uuid()`
- `code text NOT NULL UNIQUE`
- `name text NOT NULL`
- `badge text`
- `public_subtitle text`
- `description text`
- `monthly_price numeric(12,2) NOT NULL DEFAULT 0`
- `annual_price numeric(12,2) NOT NULL DEFAULT 0`
- `price_complement text`
- `cta_label text`
- `display_order int NOT NULL DEFAULT 0`
- flags públicas e internas de pricing
- `status text NOT NULL DEFAULT 'active'`
- `created_at` / `updated_at`

## 4. Schema real de `valorapesquisa.plan_limits`

Contrato oficial consolidado nesta correção:

- `plan_id uuid PRIMARY KEY REFERENCES valorapesquisa.plans(id) ON DELETE CASCADE`
- `active_surveys int NOT NULL DEFAULT 0`
- `responses_per_month int NOT NULL DEFAULT 0`
- `users int NOT NULL DEFAULT 0`
- `managers int NOT NULL DEFAULT 0`
- `forms int NOT NULL DEFAULT 0`
- `public_links int NOT NULL DEFAULT 0`
- `email_invites_per_month int NOT NULL DEFAULT 0`
- `storage_mb int NOT NULL DEFAULT 0`
- `created_at` / `updated_at`

## 5. Schema real de `valorapesquisa.plan_capabilities`

Contrato oficial consolidado nesta correção:

- `plan_id uuid NOT NULL REFERENCES valorapesquisa.plans(id) ON DELETE CASCADE`
- `capability_code text NOT NULL`
- `enabled boolean NOT NULL DEFAULT true`
- `created_at` / `updated_at`
- chave primária composta `(plan_id, capability_code)`

## 6. Schema real de `valorapesquisa.organizations`

Contrato oficial consolidado para vínculo de plano:

- `organizations.plan_code text NOT NULL DEFAULT 'free'`
- `subscriptions.plan_id uuid` referencia `plans.id`
- `subscriptions.organization_id` é único para permitir upsert idempotente por organização

## 7. INSERTs incompatíveis

Foram incompatíveis os inserts que:

- inseriam `price_label` em `plans`;
- usavam `plans.id` como código textual (`free`, `essential`, etc.);
- gravavam `plan_limits(plan_id, limit_key, limit_value)`;
- gravavam `plan_capabilities(plan_id, capability_key, capability_level, capability_type)`;
- gravavam `organizations(..., plan_id)` quando o vínculo oficial passou a ser `plan_code` e/ou `subscriptions`.

## 8. Ajustes feitos

- `plans` agora usa `code` como identificador textual idempotente.
- `price_label` foi removido dos scripts oficiais.
- `monthly_price` e `annual_price` foram adicionados ao schema e seed.
- `plan_limits` foi convertido para colunas estruturadas.
- `plan_capabilities` foi convertido para `capability_code` e `enabled`.
- Organização Valora usa `plan_code='enterprise'`.
- Assinatura Valora é criada/atualizada via `subscriptions` buscando `plans.id` por `plans.code`.
- Validadores e testes estáticos foram adicionados/atualizados.

## 9. Riscos de compatibilidade

- Bancos já criados com o contrato antigo podem precisar de migração real de dados se a tabela `plans` já existir com `id text` e `price_label`.
- Código ou relatórios externos que leem `price_label`, `limit_key` ou `capability_key` precisarão migrar para o novo contrato.
- A alteração de `subscriptions.plan_id` para UUID exige que integrações usem `plans.code` para lookup antes de gravar a FK.
