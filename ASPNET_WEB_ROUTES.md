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

## Sprint Web Legacy Public Parity
A migração da Web oficial ASP.NET somente será considerada completa quando houver paridade verificável de layout e jornada pública com o legado da raiz: Home comercial, diagnóstico gratuito, pesquisa pública, resultado, certificado, LGPD, WhatsApp/contato, footer, modal/toast/loading, ValoraBot visual, mobile-first e separação total entre `_PublicLayout` e `_AdminLayout`. A Web oficial deve consumir apenas a API oficial, sem Firebase, sem acesso direto ao banco, sem JSON bruto, sem dados fake e sem exposição de senha, hashes, tokens, segredos, connection strings ou payload sensível.

## Sprint Backend Centralização Valora Insight™

A fonte oficial da evolução Valora Insight™ passa a ser `backend/Valora.sln` e `database/postgresql`. O diagnóstico oficial usa 5 dimensões, 25 perguntas reais extraídas do legado (`app.js`), escala 1 a 5, total máximo 125 e devolutiva determinística no backend. A Web oficial não deve usar Firebase nem acessar banco diretamente; deve consumir a API oficial. Credenciais Firebase/service account devem ficar fora do repositório e a chave compartilhada fora do fluxo seguro deve ser revogada/rotacionada.

## Sprint Visual Homologation — assets da marca

Os binários oficiais `backend/Valora.Web/wwwroot/img/brand/valora-logo-full.jpeg` e `backend/Valora.Web/wwwroot/img/brand/valora-symbol.jpeg` precisam ser adicionados manualmente, pois o Codex não manipula arquivos binários de marca. A Web oficial possui fallback visual seguro com texto institucional “Valora Group”, evitando imagem quebrada e preservando layout público/admin até o upload manual.

Validação:

- `npm run web:brand-assets` é o modo padrão e falha quando os binários oficiais não existem.
- `VALORA_ALLOW_MISSING_BRAND_ASSETS=true npm run web:brand-assets` é o modo diagnóstico; ele registra a pendência manual sem falhar por ausência dos JPEGs, mas continua bloqueando `VG` como marca final, logo externa, secrets, service account e paths inseguros.
- `npm run web:visual-homologation` valida layouts, fallback, documentação, checklist, Home, diagnóstico, resultado, segurança e ausência de logo externa.

Consulte `VALORA_BRAND_ASSETS_MANUAL_SETUP.md` para nomes obrigatórios, testes locais e commit manual dos binários.

## RC2 visual — Valora Brand Assets

- A etapa visual RC2 usa os paths oficiais `/img/brand/valora-logo-full.jpeg` e `/img/brand/valora-symbol.jpeg` no projeto `backend/Valora.Web`.
- Os binários reais da marca devem ser incluídos manualmente; o Codex não cria, converte nem anexa imagens oficiais.
- Enquanto os JPEGs não estiverem versionados, as telas públicas e administrativas exibem fallback premium textual `Valora Group`, sem `VG`/`V` solto e sem imagem quebrada.
- Validação obrigatória após inclusão dos assets: `npm run web:brand-assets`.
- Validação diagnóstica sem binários: `VALORA_ALLOW_MISSING_BRAND_ASSETS=true npm run web:brand-assets`.
- Readiness visual RC2: `npm run web:rc2-visual-readiness`.
- A homologação final ainda deve ser executada com .NET SDK, PostgreSQL e navegador real para validar desktop/mobile antes do pacote `0.9.0-rc2`.
