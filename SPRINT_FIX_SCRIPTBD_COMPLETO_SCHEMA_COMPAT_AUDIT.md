# Auditoria — Sprint `scriptbd_completo.sql` compatível

## 1. Resumo

Centralizada a compatibilidade de schema antigo no bloco `-- COMPATIBILIDADE PARA BANCOS EXISTENTES`, antes de seeds e índices críticos.

## 2. Causa do erro `plan_limits.users`

A tabela `valorapesquisa.plan_limits` já existia em alguns bancos sem `users`; `CREATE TABLE IF NOT EXISTS` não adiciona colunas em tabelas existentes.

## 3. Tabelas corrigidas

`plans`, `plan_limits`, `plan_capabilities`, `organizations`, `forms`, `form_dimensions`, `questions`, `question_options`, `email_templates` e `usage_monthly`.

## 4. Bloco de compatibilidade criado

Inclui `ALTER TABLE ... ADD COLUMN IF NOT EXISTS` e índices únicos oficiais necessários para seeds idempotentes.

## 5. Seeds corrigidos

Seeds de planos, limites, capacidades, organização, Valora Insight™ e templates de e-mail usam colunas garantidas previamente.

## 6. Índices corrigidos

`idx_usage_monthly_organization_month` usa `period_month`.

## 7. Validador criado

`tools/validate-scriptbd-completo-sql-compat.js` verifica colunas antes dos seeds, índice correto, ausência de `price_label`/`badge`, ausência de `ON CONFLICT (code, organization_id)` em e-mail e ausência de service accounts/secrets óbvios.

## 8. Teste em banco limpo

Cenário documentado em `backend/database/postgresql/compat-tests/clean-db.sql`. Resultado local registrado após execução dos comandos obrigatórios.

## 9. Teste em banco antigo

Cenários locais criados em `backend/database/postgresql/compat-tests/` para `plan_limits` chave/valor, ausência de `monthly_price`, ausência de `users`, ausência de `plan_code` e `email_templates` antigo.

## 10. Resultado da primeira execução

Não executado neste container: `psql` não está instalado no PATH.

## 11. Resultado da segunda execução

Não executado neste container: `psql` não está instalado no PATH.

## 12. Comandos executados

- `npm run db:scriptbd-validate`
- `npm run backend:sql-schema-validate`
- `npm run backend:official-validate`
- `npm run check:critical`
- `command -v psql`
- `command -v dotnet`

## 13. Comandos não executados e motivo

- `psql -U postgres -d postgres -f scriptbd_completo.sql` primeira e segunda execução: não executado porque `psql` não está instalado.
- Consultas SQL de contagem: não executadas porque `psql` não está instalado.
- `dotnet restore backend/Valora.sln`, `dotnet build backend/Valora.sln`, `dotnet test backend/Valora.sln`: não executados porque `dotnet` não está instalado.

## 14. Gaps restantes

Validar em base de homologação com backup recente antes de produção.

## 15. Próximo passo recomendado

Executar o script duas vezes em PostgreSQL real de homologação e comparar contagens oficiais após backup.
