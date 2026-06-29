# KNOWN LIMITATIONS BEFORE PRODUCTION.md

Documento atualizado na Sprint 43.

- Front oficial: `backend/Valora.Web`.
- Stack permitida: ASP.NET Core MVC/Razor Pages, Bootstrap 5, JavaScript puro, jQuery e AJAX.
- Node permitido apenas para scripts, validadores, automação, Playwright e gates.
- Swagger corrigido sem `ConflictingActionsResolver`.
- Endpoint oficial de resultado: `GET /responses/{responseId}/result`.
- Gaps operacionais restantes estão centralizados em `ASPNET_WEB_API_GAPS.md`.

## Sprint 44 — SMTP e CSP

- Aplicar `database/postgresql/044_email_jobs_smtp_real.sql` antes do go-live SMTP.
- Configurar variáveis `Email__Smtp__*` no IIS/Windows sem commitar senha.
- Remover rota compatível obsoleta `/communication/result/send` após clientes migrarem.
