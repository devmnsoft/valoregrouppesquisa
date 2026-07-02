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
