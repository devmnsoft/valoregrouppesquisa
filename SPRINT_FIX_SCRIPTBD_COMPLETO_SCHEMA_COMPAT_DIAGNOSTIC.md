# Diagnóstico — Compatibilidade `scriptbd_completo.sql`

## Erro atual

`ERROR: column "users" of relation "plan_limits" does not exist` no seed de `valorapesquisa.plan_limits`.

## Causa

`CREATE TABLE IF NOT EXISTS` cria a tabela quando ela não existe, mas não altera tabelas antigas. Bancos já provisionados com `plan_limits` sem `users` falhavam quando o seed tentava inserir nessa coluna.

## Tabelas criadas com `CREATE TABLE IF NOT EXISTS`

O script oficial cria tabelas de identidade, planos, formulários, pesquisas, respostas, resultados, certificados, comunicação, auditoria, operação, importação, relatórios/LGPD e migração com `CREATE TABLE IF NOT EXISTS`. A lista completa é verificável com:

```bash
rg -n "CREATE TABLE IF NOT EXISTS" scriptbd_completo.sql backend/database/postgresql/*.sql
```

## INSERTs sensíveis a schemas antigos

- `plans`: usa `monthly_price`, `annual_price`, `display_order`, `status`.
- `plan_limits`: usa `users` e demais limites oficiais.
- `plan_capabilities`: usa `capability_code` e `enabled`.
- `organizations`: usa `plan_code`.
- `forms`, `form_dimensions`, `questions`, `question_options`: usam nomes legados e novos.
- `email_templates`: usa `body_html`/`body_text` e também mantém compatibilidade com `body`.

## Índices sensíveis a schemas antigos

- `usage_monthly`: corrigido para `period_month`.
- Índices de `created_at`/`status` agora são precedidos pelo bloco de compatibilidade.

## Conflitos entre schema antigo e novo

- `plan_limits`: modelo antigo chave/valor versus modelo oficial por `plan_id`.
- `plan_capabilities`: modelo antigo `capability_key`/níveis versus `capability_code` booleano.
- `plans`: versões sem `monthly_price`/`annual_price`.
- `forms/questions/options`: contratos com `title/name`, `position/display_order`, `label/text`, `value/score`.
- `email_templates`: contrato operacional (`body`) versus contrato HTML/texto (`body_html`, `body_text`).

## Plano de compatibilidade

Foi criado bloco oficial `-- COMPATIBILIDADE PARA BANCOS EXISTENTES` antes dos seeds, com `ALTER TABLE ... ADD COLUMN IF NOT EXISTS`, índices únicos oficiais e normalizações não destrutivas. Tabelas de negócio não são removidas. Tabelas de configuração antigas recebem colunas oficiais para que os seeds idempotentes convertam o conteúdo ao contrato atual.
