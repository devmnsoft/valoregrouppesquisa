# Checklist de hardening de segurança — backend oficial

- [ ] Headers de segurança habilitados e validados.
- [ ] CORS restrito por ambiente.
- [ ] Cookies `HttpOnly`, `Secure` e `SameSite` quando aplicável.
- [ ] JWT com expiração, issuer/audience e segredo fora do código.
- [ ] Rate limit em endpoints públicos.
- [ ] Rate limit e autorização forte em importação.
- [ ] Tamanho máximo de upload configurado (`VALORA_MIGRATION_MAX_FILE_MB`).
- [ ] Erros sanitizados sem stack trace em resposta pública.
- [ ] Logs sem senha, token, hash, connection string, payload sensível ou e-mail completo.
- [ ] Endpoints administrativos com `[Authorize]`.
- [ ] Endpoints críticos restritos a `admin_valora`.
- [ ] Antiforgery nas telas MVC com POST/ações críticas.
- [ ] Backup/restore protegidos por confirmação explícita.
- [ ] Importação real exige dry-run, relatório, ausência de conflito bloqueante, batch, rollback e auditoria.

## Release Candidate 0.9.0-rc1

Esta documentação passa a considerar o Release Candidate `0.9.0-rc1` como pacote de homologação real da versão oficial localizada em `backend/Valora.sln` e `database/postgresql`. O legado e `backend-v2` permanecem apenas como referência histórica e não fazem parte do build oficial.

Antes de produção, execute em ambiente completo: `dotnet restore backend/Valora.sln`, `dotnet build backend/Valora.sln`, `dotnet test backend/Valora.sln`, validadores npm oficiais, PostgreSQL homologação, health checks HTTP, fluxos SaaS/pesquisa/relatórios/LGPD/e-mail/importação, backup/restore descartável e scripts `tools/*/backend-prd-*`.

Não versionar `.env`, dumps, logs sensíveis, dados reais, certificados reais ou secrets. Não executar cutover produtivo automático; seguir `CUTOVER_PLAN.md`, `ROLLBACK_PLAN.md`, `BACKUP_RESTORE_RUNBOOK.md`, `RELEASE_CANDIDATE_NOTES.md` e `PILOT_USERS_HOMOLOGATION_PLAN.md`.
