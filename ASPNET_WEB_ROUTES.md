# ASPNET WEB ROUTES.md

Documento atualizado na Sprint 43.

- Front oficial: `backend/Valora.Web`.
- Stack permitida: ASP.NET Core MVC/Razor Pages, Bootstrap 5, JavaScript puro, jQuery e AJAX.
- Node permitido apenas para scripts, validadores, automação, Playwright e gates.
- Swagger corrigido sem `ConflictingActionsResolver`.
- Endpoint oficial de resultado: `GET /responses/{responseId}/result`.
- Gaps operacionais restantes estão centralizados em `ASPNET_WEB_API_GAPS.md`.

## Rotas oficiais após consolidação

As rotas oficiais da nova versão .NET devem ser servidas por `backend/Valora.Api` e consumidas por `backend/Valora.Web`. `backend-v2` não é fonte oficial de rotas. Rotas públicas existentes de pesquisa e resultado não devem ser quebradas durante a consolidação.

## Rotas operacionais oficiais
- `/reports`, `/reports/generated`, `/reports/surveys/{surveyId}`, `/reports/responses/{responseId}`, `/reports/organization`
- `/certificates`, `/certificates/responses/{responseId}/generate`, `/public/certificates/validate`
- `/exports`, `/exports/{id}/download`
- `/public/lgpd/requests`, `/lgpd/consents`, `/lgpd/privacy-requests`
- `/email/templates`, `/email/jobs`, `/email/status`, `/email/jobs/process`

## Rotas MVC de migração

- `/Migration`
- `/Migration/Batches`
- `/Migration/Batch/{id}`
- `/Migration/Upload`
- `/Migration/DryRun/{batchId}`
- `/Migration/Conflicts/{batchId}`
- `/Migration/Reconciliation/{batchId}`
- `/Migration/Rollback/{batchId}`
- `/Migration/CutoverReadiness/{batchId}`

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
