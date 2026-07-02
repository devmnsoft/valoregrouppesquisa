# Checklist de homologação e cutover assistido

## Login e segurança
- [ ] Login admin. - [ ] Login empresa. - [ ] Acesso negado por perfil. - [ ] Menu por perfil. - [ ] Logout.

## Operação SaaS
- [ ] Plano. - [ ] Módulo. - [ ] Bloqueio por limite. - [ ] Dashboard. - [ ] Uso mensal.

## Pesquisa pública
- [ ] Validar link. - [ ] Responder. - [ ] Ver resultado. - [ ] Gerar certificado. - [ ] Enviar resultado por e-mail.

## Administração
- [ ] Formulário. - [ ] Pesquisa. - [ ] Link. - [ ] Respostas. - [ ] Relatórios. - [ ] Exportações. - [ ] LGPD. - [ ] E-mail jobs. - [ ] Auditoria.

## Migração
- [ ] Upload. - [ ] Dry-run. - [ ] Conflitos. - [ ] Apply. - [ ] Conciliação. - [ ] Rollback. - [ ] Readiness.

## Produção
- [ ] Backup. - [ ] Restore testado. - [ ] Health checks. - [ ] Logs. - [ ] Monitoramento. - [ ] Rollback.

## Release Candidate 0.9.0-rc1

Esta documentação passa a considerar o Release Candidate `0.9.0-rc1` como pacote de homologação real da versão oficial localizada em `backend/Valora.sln` e `database/postgresql`. O legado e `backend-v2` permanecem apenas como referência histórica e não fazem parte do build oficial.

Antes de produção, execute em ambiente completo: `dotnet restore backend/Valora.sln`, `dotnet build backend/Valora.sln`, `dotnet test backend/Valora.sln`, validadores npm oficiais, PostgreSQL homologação, health checks HTTP, fluxos SaaS/pesquisa/relatórios/LGPD/e-mail/importação, backup/restore descartável e scripts `tools/*/backend-prd-*`.

Não versionar `.env`, dumps, logs sensíveis, dados reais, certificados reais ou secrets. Não executar cutover produtivo automático; seguir `CUTOVER_PLAN.md`, `ROLLBACK_PLAN.md`, `BACKUP_RESTORE_RUNBOOK.md`, `RELEASE_CANDIDATE_NOTES.md` e `PILOT_USERS_HOMOLOGATION_PLAN.md`.
