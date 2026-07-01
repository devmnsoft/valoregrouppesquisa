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
