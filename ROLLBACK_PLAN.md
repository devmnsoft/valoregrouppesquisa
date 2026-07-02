# Plano de rollback produtivo

- Acionar quando houver indisponibilidade crítica, corrupção/dados divergentes, falha de segurança ou falha de aceite.
- Aprovação por responsável técnico e negócio.
- Restaurar backup conforme `BACKUP_RESTORE_RUNBOOK.md`, preservando evidências antes de qualquer alteração.
- Desfazer importação por batch usando relatório de rollback e restore quando necessário.
- Reativar legado preservado/read-only conforme decisão formal.
- Validar retorno: login, pesquisa pública, relatórios, e-mail, certificados e health.
- Comunicar usuários com linguagem clara e sem detalhes sensíveis.
- Registrar incidente com linha do tempo, causa provável, impacto, evidências e ações.
- Preservar dumps, logs sanitizados, relatórios de dry-run/apply/conciliação/rollback.
