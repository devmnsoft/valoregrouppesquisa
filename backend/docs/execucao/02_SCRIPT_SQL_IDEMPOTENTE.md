# Execução idempotente do SQL

Use o script canônico sem editar dados reais:

```bash
psql "$VALORA_POSTGRES" -v ON_ERROR_STOP=1 -f database/postgresql/script_completo.sql
psql "$VALORA_POSTGRES" -v ON_ERROR_STOP=1 -f database/postgresql/script_completo.sql
```

O script usa criação condicional, blocos `DO`, inspeção de `information_schema` e índices condicionais. A migração de notificações adota `message`; `body` somente pode ser lido por SQL dinâmico após verificar a coluna. A estrutura segura de chaves usa `key_hash` e migra nomes legados condicionalmente.

Antes de homologar, execute em banco novo e em cópia sanitizada de banco existente. Não use `DROP TABLE` nem remova dados para contornar incompatibilidades. Registre data, versão PostgreSQL e resultado das duas passagens.
