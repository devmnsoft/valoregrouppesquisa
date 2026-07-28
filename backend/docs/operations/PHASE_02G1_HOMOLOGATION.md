# Homologação da fase 02G.1

Ordem obrigatória: restore, build, testes Unit, testes Architecture/StaticContract, bootstrap PostgreSQL duas vezes, migrations, DatabaseContract/Integration, gate 2G.1, format e publicação dos artefatos.

Rollback de implantação: interromper consumidores de outbox, reverter o binário e manter as colunas canônicas (a migração é preservadora). Não apagar organizações nem restaurar colunas históricas sem backup validado.
