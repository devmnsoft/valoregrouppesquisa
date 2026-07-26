# Guia de compatibilidade do scriptbd_completo.sql

O script completo oficial é `scriptbd_completo.sql`, espelhado em `backend/database/postgresql/scriptbd_completo.sql`.

## Banco antigo
`CREATE TABLE IF NOT EXISTS` não adiciona colunas em tabelas já existentes. Por isso o bloco `-- COMPATIBILIDADE PARA BANCOS EXISTENTES` deve ficar antes dos triggers, índices e seeds.

## Caso `plan_limits.users`
A coluna `plan_limits.users` precisa ser criada por `ALTER TABLE valorapesquisa.plan_limits ADD COLUMN IF NOT EXISTS users int NOT NULL DEFAULT 0;` antes de qualquer `INSERT INTO valorapesquisa.plan_limits`.

## Índice usage_monthly
O índice correto usa `period_month`: `usage_monthly(organization_id, period_month)`.

## Validação
Execute `npm run db:scriptbd-validate` antes de homologar banco limpo, banco antigo e segunda execução.
