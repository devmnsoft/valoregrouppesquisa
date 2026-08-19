# Testes SQL

## Nível 1 — sempre executado

`run_sql_static_checks.ps1` e `CanonicalSqlSafetyTests` leem `database/postgresql/script_completo.sql`, removem comentários antes de procurar `DROP TABLE`/`TRUNCATE`, exigem guardas em `CREATE TABLE` de nível superior e verificam os contratos de hash de API keys e mensagem de notificações. SQL dinâmico dentro de blocos condicionais é analisado pelos testes de contrato existentes.

## Nível 2 — opcional

A execução real requer `VALORA_TEST_POSTGRES_CONNECTION` para base isolada com nome inequivocamente de teste/QA/homologação. O operador deve conferir host e database antes de executar. O script deve ser aplicado duas vezes para provar reexecução. Não há criação, exclusão ou limpeza automática do banco.
