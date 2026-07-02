# Sprint Fix — Auditoria de colunas SQL de `valorapesquisa.plans`

## 1. Causa do erro

Os seeds oficiais de planos ainda usavam um contrato antigo da tabela `valorapesquisa.plans`, com colunas de apresentação pública que não fazem parte do schema oficial harmonizado. Isso causava falhas como:

- `ERROR: column "price_label" of relation "plans" does not exist`
- `ERROR: column "badge" of relation "plans" does not exist`

A correção removeu o acoplamento dos seeds a campos artificiais/legados e manteve o seed limitado ao contrato real da tabela.

## 2. Colunas inexistentes removidas/bloqueadas

As referências abaixo foram removidas dos scripts SQL oficiais e passaram a ser bloqueadas pelo validador:

- `price_label`
- `badge`
- `public_subtitle`
- `public_description`
- `highlight_text`
- `cta_label`

Além disso, os seeds oficiais de planos não usam mais campos legados de UI/comercialização que não pertencem ao contrato mínimo do backend, como `price_complement`, `highlight`, `recommended`, `visible_on_public_pricing` e `internal_only`.

## 3. Scripts corrigidos

- `scriptbd_completo.sql`
  - Schema de `valorapesquisa.plans` harmonizado.
  - Seed oficial de planos ajustado para usar apenas colunas reais.
  - `ON CONFLICT (code)` mantido para idempotência.

- `database/postgresql/scriptbd_completo.sql`
  - Mesmas correções aplicadas ao script completo oficial dentro de `database/postgresql`.

- `database/postgresql/003_plan_tables.sql`
  - Tabela `valorapesquisa.plans` reduzida ao contrato real usado pelo backend.

- `database/postgresql/011_seed_official_plans.sql`
  - Seed oficial de planos ajustado para colunas reais.
  - Upsert por `code` mantido.

- `database/postgresql/099_seed_e2e_live_fixture.sql`
  - Revisado: já usava `code`, preços, ordem, status, limites estruturados e `plan_code` em organizações.

## 4. Schema real considerado

O schema oficial considerado para `valorapesquisa.plans` contém:

- `id uuid PRIMARY KEY DEFAULT gen_random_uuid()`
- `code text NOT NULL UNIQUE`
- `name text NOT NULL`
- `description text`
- `monthly_price numeric(12,2) NOT NULL DEFAULT 0`
- `annual_price numeric(12,2) NOT NULL DEFAULT 0`
- `display_order int NOT NULL DEFAULT 0`
- `status text NOT NULL DEFAULT 'active'`
- `created_at timestamptz NOT NULL DEFAULT now()`
- `updated_at timestamptz NOT NULL DEFAULT now()`

Os seeds de planos foram alinhados a esse contrato, sem adicionar colunas artificiais para mascarar erro de seed.

## 5. Validador criado/atualizado

O validador `tools/validate-backend-official-sql-schema.js` foi reforçado para:

- localizar o schema oficial de `valorapesquisa.plans`;
- bloquear criação ou uso de colunas inexistentes/listadas como proibidas;
- localizar todos os `INSERT INTO valorapesquisa.plans(...)` nos SQLs oficiais;
- validar que cada insert usa apenas colunas presentes no schema real;
- exigir `code` nos inserts de planos;
- impedir seed textual em `id` UUID;
- garantir `ON CONFLICT (code) DO UPDATE`;
- manter as proteções contra contratos antigos de:
  - `plan_limits(limit_key, limit_value)`;
  - `plan_capabilities(capability_key, capability_level, capability_type)`;
  - `organizations(plan_id)` quando o schema usa `plan_code`.

Também foi adicionado o script npm:

```bash
npm run backend:official-sql-schema
```

## 6. Comandos executados

- `npm run backend:official-sql-schema`
- `npm run backend:official-validate`
- `node --check tools/validate-backend-official-sql-schema.js`
- `rg -n "INSERT INTO valorapesquisa\.plans|CREATE TABLE IF NOT EXISTS valorapesquisa\.plans|price_label|badge|public_subtitle|public_description|highlight_text|cta_label" scriptbd_completo.sql database/postgresql tools/validate-backend-official-sql-schema.js package.json`

## 7. Gaps restantes

- Não foi executado um banco PostgreSQL real nesta correção; a validação foi estática pelos validadores oficiais disponíveis no repositório.
- As ocorrências dos nomes proibidos permanecem apenas dentro da lista de bloqueio do próprio validador, para impedir regressões futuras.
- Scripts arquivados em `database/postgresql_archive` não foram alterados porque não fazem parte do conjunto oficial solicitado para execução/correção.
