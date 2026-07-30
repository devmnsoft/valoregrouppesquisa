# Testes locais de compatibilidade do script_completo.sql

Cenários oficiais para PostgreSQL real:

1. `clean-db.sql`: banco limpo.
2. `legacy-plan-limits-key-value.sql`: `plan_limits(limit_key, limit_value)`.
3. `legacy-plans-without-monthly-price.sql`: `plans` sem `monthly_price`.
4. `legacy-plan-limits-without-users.sql`: `plan_limits` sem `users`.
5. `legacy-organizations-without-plan-code.sql`: `organizations` sem `plan_code`.
6. `legacy-email-templates.sql`: `email_templates` com contrato antigo.

Execute cada arquivo em uma base descartável e depois rode `psql -U postgres -d postgres -f script_completo.sql` duas vezes.
