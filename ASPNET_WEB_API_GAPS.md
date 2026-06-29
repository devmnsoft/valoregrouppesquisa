# ASP.NET Web API Gaps

## Gaps controlados atuais
Os endpoints administrativos antigos em `WebAdminModulesController` não retornam mais dados fake como produção. Quando ainda não há repository real, retornam HTTP 501 com `WEB_ADMIN_REAL_REPOSITORY_REQUIRED`.

## Repositories pendentes
OrganizationRepository, UserRepository, FormRepository, SurveyRepository, SurveyLinkRepository, ResponseRepository, AuditRepository, SettingsRepository e UsageRepository precisam ser concluídos para produção plena.

## Regras de segurança monitoradas
- `PLAN_LIMIT_REACHED` deve ser retornado por comandos que ultrapassem limites de plano.
- `resultToken` não deve ser persistido em localStorage.
- `result_token_hash`, `password_hash` e `token_hash` não devem aparecer na UI.
- Escopo por `organizationId`, permissões por role e módulos por plano continuam obrigatórios.

## Sprint 44

O Valora.Web permanece como frontend oficial ASP.NET Core e usa `/communications/result/{responseId}/send-email`, `/admin/email-jobs`, `/admin/email-jobs/process` e `/admin/email/config/status`.
