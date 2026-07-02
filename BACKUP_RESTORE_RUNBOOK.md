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
