# Backup e restore PostgreSQL
## Backup
Execute com conta de leitura, destino criptografado e fora do servidor: `pg_dump --format=custom --no-owner --no-acl --file valora_TIMESTAMP.dump "$CONNECTION_STRING"`. Calcule checksum, retenha conforme política e teste restauração periodicamente. Não registre dump, senha ou caminho sensível no banco.

## Restore assistido
Abra janela de mudança, habilite manutenção, interrompa writers, crie banco vazio compatível e execute `pg_restore --clean --if-exists --no-owner --no-acl --dbname "$TARGET_CONNECTION_STRING" valora_TIMESTAMP.dump`. Valide checksum, schema version, health, contagens e smoke antes do cutover. Restore nunca é automático.

Após cada operação, registre status, executor, data, notas não sensíveis e correlationId em `backup_events`, `restore_events` e governança. Atualize `Backup__LastKnownAt`. Falha ou ausência de backup em produção é crítica.
