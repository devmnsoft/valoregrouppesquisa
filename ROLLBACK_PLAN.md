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

## Release Candidate 0.9.0-rc1

Esta documentação passa a considerar o Release Candidate `0.9.0-rc1` como pacote de homologação real da versão oficial localizada em `backend/Valora.sln` e `database/postgresql`. O legado e `backend-v2` permanecem apenas como referência histórica e não fazem parte do build oficial.

Antes de produção, execute em ambiente completo: `dotnet restore backend/Valora.sln`, `dotnet build backend/Valora.sln`, `dotnet test backend/Valora.sln`, validadores npm oficiais, PostgreSQL homologação, health checks HTTP, fluxos SaaS/pesquisa/relatórios/LGPD/e-mail/importação, backup/restore descartável e scripts `tools/*/backend-prd-*`.

Não versionar `.env`, dumps, logs sensíveis, dados reais, certificados reais ou secrets. Não executar cutover produtivo automático; seguir `CUTOVER_PLAN.md`, `ROLLBACK_PLAN.md`, `BACKUP_RESTORE_RUNBOOK.md`, `RELEASE_CANDIDATE_NOTES.md` e `PILOT_USERS_HOMOLOGATION_PLAN.md`.
