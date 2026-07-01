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
