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
