# Execução do script completo

Execute em PostgreSQL 15 ou 16:

```bash
psql "$VALORA_TEST_POSTGRES_CONNECTION" -v ON_ERROR_STOP=1 -f backend/database/postgresql/script_completo.sql
```

Reaplique o mesmo comando para atualizar ou provar idempotência. Não execute migrations adicionais. Faça backup e ensaio em homologação antes de produção.
