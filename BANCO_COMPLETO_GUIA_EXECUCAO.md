# Guia de execução — `database/postgresql/banco_completo.sql`

## Requisitos

- PostgreSQL 15 ou superior.
- Usuário com permissão para `CREATE SCHEMA`, `CREATE EXTENSION`, `CREATE TABLE`, `CREATE INDEX`, `CREATE FUNCTION` e `CREATE TRIGGER`.
- Backup antes de qualquer execução fora de ambiente descartável.

## Criação e conexão

```bash
createdb valoradb
psql "postgresql://usuario:senha@host:5432/valoradb"
```

## Execução

```bash
psql "postgresql://usuario:senha@host:5432/valoradb" -v ON_ERROR_STOP=1 -f database/postgresql/banco_completo.sql
```

## Reexecução

O script foi escrito para ser idempotente: usa `CREATE ... IF NOT EXISTS`, `CREATE OR REPLACE FUNCTION`, índices condicionais, triggers recriados de forma determinística e seeds com `ON CONFLICT`.

```bash
psql "postgresql://usuario:senha@host:5432/valoradb" -v ON_ERROR_STOP=1 -f database/postgresql/banco_completo.sql
```

## Validação

- Confirmar schema: `select schema_name from information_schema.schemata where schema_name = 'valorapesquisa';`
- Confirmar planos: `select code,is_public,is_active,is_legacy from valorapesquisa.plans order by code;`
- Confirmar pesquisa: consultar `forms`, `form_versions`, `dimensions` e `questions`.
- Confirmar idempotência: executar o script duas vezes com `ON_ERROR_STOP=1`.

## Backup

Antes de produção:

```bash
pg_dump --format=custom --file=backup_valoradb.dump valoradb
```

## Cuidados de produção

- Não executar automaticamente o bootstrap completo na inicialização da aplicação.
- Executar manualmente ou via pipeline controlado.
- Validar checksum e registrar aplicação em `valorapesquisa.schema_migrations`.
- Aplicar lock transacional/advisory lock nos scripts incrementais.
- Nunca usar credenciais reais em arquivos versionados.

## Scripts incrementais

- `banco_completo.sql` é o bootstrap para bancos vazios ou normalização inicial controlada.
- Evoluções futuras devem entrar como migrations versionadas pequenas.
- Cada migration deve registrar `version`, `checksum`, data de aplicação e executor em `schema_migrations`.
- Rollback deve ser documentado quando tecnicamente seguro.
