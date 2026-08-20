# Runbook de segurança e produção

## Configuração obrigatória

- Injete `ConnectionStrings__Postgres`, `Jwt__SigningKey`, credenciais SMTP e chaves externas pelo cofre do ambiente. Nunca use arquivos versionados.
- Configure `Cors__AllowedOrigins__0` com a origem HTTPS exata do `Valora.Web`; produção não aceita curingas. O navegador consome o BFF em `/bff`, sem URL HTTP cruzada.
- Configure persistência compartilhada das chaves de Data Protection ao executar mais de uma instância Web.
- Mantenha `Authentication__SessionMinutes` entre 5 e 720 minutos e sincronize relógios por NTP.
- Restrinja `/health/ready` à sonda/rede operacional no gateway. O endpoint só publica estados sanitizados.

## Backup, retenção e recuperação

1. Execute `VALORA_DATABASE_URL=... VALORA_BACKUP_DIR=... backend/scripts/operations/backup.sh` em agenda diária.
2. Armazene cópia criptografada, imutável e fora da região primária. A retenção padrão é 30 dias e pode ser alterada por `VALORA_BACKUP_RETENTION_DAYS`.
3. Mensalmente restaure em banco vazio isolado com `VALORA_RESTORE_DATABASE_URL=... backend/scripts/operations/restore.sh backup.dump`.
4. Aplique `script_completo.sql` duas vezes, rode testes de integridade e compare contagens por organização. Registre evidência, RPO, RTO, operador e correlation ID.
5. Nunca restaure dados reais em desenvolvimento; use anonimização antes de disponibilizar uma cópia.

## Resposta a incidentes e LGPD

- Preserve os logs append-only, correlation ID e trilha de auditoria; não copie tokens, respostas abertas, senhas ou documentos para tickets.
- Revogue sessões e tokens afetados, isole a organização e registre início, escopo, mitigação e encerramento.
- Solicitações de titular devem seguir exclusão lógica/anonimização e retenção legal. Relatórios anônimos não podem receber nome, e-mail, telefone, IP ou resposta aberta identificável.

## E2E mínimo de liberação

Em tenant descartável: login `admin_valora`; criar organização e usuário; atribuir plano e permissões; criar/publicar diagnóstico; responder token público; gerar resultado, relatório e certificado; validar certificado; baixar o relatório autenticado; confirmar os eventos na auditoria. Depois tente cada leitura/download com outro `organizationId`, token expirado e token revogado, esperando `403`/`404` sem stack trace.

Antes de promover, valide desktop e mobile das páginas 400, 401, 403, 404 e 500, inspecione headers, confirme `/health/ready` e monitore workers/outbox até não haver retry recorrente.
