# Guia do `script_completo.sql`

Execute com um usuário autorizado, após backup do banco existente:

```bash
psql "$CONNECTION_STRING" -v ON_ERROR_STOP=1 -f backend/database/postgresql/script_completo.sql
```

O script usa operações idempotentes como `IF NOT EXISTS`; mensagens `already exists, skipping` são notas do PostgreSQL e não representam falha. Com `ON_ERROR_STOP=1`, um erro real encerra a execução e deve ser investigado antes de uma nova tentativa.

## Migração de notificações

O campo canônico é `valorapesquisa.notifications.message`. A migração consulta `information_schema` antes de mencionar a coluna histórica `body` e somente executa a cópia por SQL dinâmico quando ela existe. Em seguida tenta preencher mensagens vazias por `title` e finalmente por `Notificação`, antes de aplicar `NOT NULL`. A coluna histórica não é apagada e nenhum dado é descartado.

Se uma versão antiga ainda produzir `column "body" does not exist`, confirme que está executando o arquivo versionado atual e procure referências diretas com:

```bash
rg -n "message\\s*=\\s*body|notifications.*body" backend/database/postgresql/script_completo.sql
```

A única ocorrência admissível de `message=body` fica dentro do `EXECUTE` protegido pelo teste de existência.

