# SAAS FINAL ACCEPTANCE CHECKLIST.md

Documento atualizado na Sprint 43.

- Front oficial: `backend/Valora.Web`.
- Stack permitida: ASP.NET Core MVC/Razor Pages, Bootstrap 5, JavaScript puro, jQuery e AJAX.
- Node permitido apenas para scripts, validadores, automação, Playwright e gates.
- Swagger corrigido sem `ConflictingActionsResolver`.
- Endpoint oficial de resultado: `GET /responses/{responseId}/result`.
- Gaps operacionais restantes estão centralizados em `ASPNET_WEB_API_GAPS.md`.

## Checklist adicional — backend oficial

- [x] `backend/Valora.sln` documentado como solução oficial.
- [x] `backend-v2` documentado apenas como referência temporária.
- [x] Validador `backend:official-validate` adicionado ao `package.json`.
- [x] DTOs oficiais de consolidação não incluem `password_hash`, `token_hash` ou `result_token_hash`.
- [ ] Implementar repositories/services completos para módulos, assinatura, uso, dashboard e menu dinâmico.
- [ ] Executar `dotnet build backend/Valora.sln` e `dotnet test backend/Valora.sln` em ambiente com SDK .NET instalado.

## Sprint operacional oficial
- [x] Repositories/services SaaS oficiais declarados para módulos, assinatura, uso, entitlements, dashboard e menu.
- [x] SQL oficial para relatórios, certificados, exportações, LGPD e e-mail.
- [x] DTOs seguros sem senha/hash/token/segredo SMTP.
- [x] Web MVC/Razor com telas operacionais.
- [x] Validador `backend:reports-email-validate`.
- [ ] Build/test .NET em ambiente com SDK disponível.

## Checklist adicional — importação controlada

- [ ] Dry-run executado antes de qualquer apply.
- [ ] Conflitos bloqueantes resolvidos ou batch bloqueado.
- [ ] Relatório de divergências revisado.
- [ ] Apply confirmado por `admin_valora`.
- [ ] Rollback por batch planejado e validado.
- [ ] Cutover readiness sem bloqueadores.

## Sprint homologação/cutover backend oficial (2026-07-02)

- Homologação local: copie `.env.example` para `.env`, ajuste apenas credenciais locais e execute `tools/linux/backend-hml-01-subir-postgres.sh` ou `tools\windows\backend-hml-01-subir-postgres.bat`.
- Banco PostgreSQL: aplique `database/postgresql/scriptbd_completo.sql` e migrações com `tools/linux/backend-hml-02-aplicar-banco.sh` ou `tools\windows\backend-hml-02-aplicar-banco.bat`.
- API/Web: rode `tools/linux/backend-hml-03-rodar-api.sh` e `tools/linux/backend-hml-04-rodar-web.sh` (equivalentes Windows disponíveis).
- Testes integrados: defina `VALORA_TEST_POSTGRES_CONNECTION` somente para base local/homologação descartável e execute `dotnet test backend/Valora.sln` em ambiente com .NET SDK 8.
- Migração com amostra: use `docs/migration-samples/*.json`; todo apply real exige dry-run, relatório de divergências, ausência de conflito bloqueante, confirmação explícita, batch, rollback e auditoria.
- Backup: execute `tools/linux/backend-hml-06-backup.sh` ou `tools\windows\backend-hml-06-backup.bat`; dumps ficam fora do versionamento.
- Restore: use `BACKUP_RESTORE_RUNBOOK.md`; exige `CONFIRM_RESTORE=RESTORE_LOCAL_HML` e, em produção, também `CONFIRM_PRODUCTION_RESTORE=true`.
- Health: valide `/health`, `/health/database`, `/health/migration`, `/health/email`, `/health/storage` e `/health/version`; na Web MVC use `/Operations/Health`, `/Operations/Version` e `/Operations/Checks`.
- Checklist/cutover/rollback: siga `HOMOLOGACAO_CUTOVER_CHECKLIST.md`, `CUTOVER_PLAN.md`, `ROLLBACK_PLAN.md` e `LEGACY_RETIREMENT_PLAN.md`. O cutover não é automático nesta sprint.
- Validação: execute `npm run backend:homologation-cutover-validate` junto dos validadores oficiais.
