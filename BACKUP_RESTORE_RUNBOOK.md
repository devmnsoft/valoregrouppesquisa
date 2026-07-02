# Runbook de backup e restore PostgreSQL — Valora backend oficial

## Variáveis
Use `.env.example` como base e crie `.env` local. Nunca versione senhas reais, dumps ou connection strings produtivas.

- `VALORA_BACKUP_DIR`: pasta de dumps, padrão `./backups/postgresql`.
- `VALORA_POSTGRES_HOST`, `VALORA_POSTGRES_PORT`, `VALORA_POSTGRES_DB`, `VALORA_POSTGRES_USER`, `VALORA_POSTGRES_PASSWORD`.
- `VALORA_RESTORE_FILE`: dump a restaurar.
- `CONFIRM_RESTORE=RESTORE_LOCAL_HML`: confirmação obrigatória para restore local/hml.
- `CONFIRM_PRODUCTION_RESTORE=true`: trava adicional para produção.

## Backup
Linux: `tools/linux/backend-hml-06-backup.sh`.
Windows: `tools/windows/backend-hml-06-backup.bat`.

O script gera dump custom (`pg_dump -Fc`) com timestamp, valida arquivo não vazio e registra log local. DUMPS NÃO DEVEM SER COMMITADOS.

## Restore
Linux: `CONFIRM_RESTORE=RESTORE_LOCAL_HML VALORA_RESTORE_FILE=... tools/linux/backend-hml-07-restore.sh`.
Windows: `set CONFIRM_RESTORE=RESTORE_LOCAL_HML` e execute `tools\windows\backend-hml-07-restore.bat`.

Restore substitui objetos no banco de destino com `pg_restore --clean --if-exists`. Não execute contra produção sem aprovação formal e `CONFIRM_PRODUCTION_RESTORE=true`.

## Auditoria e evidência
Registre operador, timestamp, ambiente, hash/tamanho do dump, comando utilizado, resultado e evidência de health pós-restore. Não publique e-mails completos, tokens ou connection strings.

## Release Candidate 0.9.0-rc1

Esta documentação passa a considerar o Release Candidate `0.9.0-rc1` como pacote de homologação real da versão oficial localizada em `backend/Valora.sln` e `database/postgresql`. O legado e `backend-v2` permanecem apenas como referência histórica e não fazem parte do build oficial.

Antes de produção, execute em ambiente completo: `dotnet restore backend/Valora.sln`, `dotnet build backend/Valora.sln`, `dotnet test backend/Valora.sln`, validadores npm oficiais, PostgreSQL homologação, health checks HTTP, fluxos SaaS/pesquisa/relatórios/LGPD/e-mail/importação, backup/restore descartável e scripts `tools/*/backend-prd-*`.

Não versionar `.env`, dumps, logs sensíveis, dados reais, certificados reais ou secrets. Não executar cutover produtivo automático; seguir `CUTOVER_PLAN.md`, `ROLLBACK_PLAN.md`, `BACKUP_RESTORE_RUNBOOK.md`, `RELEASE_CANDIDATE_NOTES.md` e `PILOT_USERS_HOMOLOGATION_PLAN.md`.
