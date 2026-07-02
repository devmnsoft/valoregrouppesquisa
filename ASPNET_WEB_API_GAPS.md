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

## Consolidação oficial `backend` x `backend-v2`

- A base oficial passa a ser exclusivamente `backend/Valora.sln`.
- `backend-v2` permanece apenas como referência temporária e não deve receber novas features.
- Gaps administrativos só podem permanecer se retornarem erro controlado e estiverem descritos neste documento.
- `WEB_ADMIN_REAL_REPOSITORY_REQUIRED` continua permitido apenas para endpoints ainda sem repository real, sem dados fake e com mensagem operacional segura.
- Próximas remoções de gap: módulos, assinatura, uso, dashboard e menu dinâmico com repositories/services oficiais.

## Sprint SaaS + Reports/Certificates/LGPD/E-mail
- Gaps de módulos, assinatura, uso, dashboard e menu foram endereçados com repositories/services oficiais em `backend`.
- `WEB_ADMIN_REAL_REPOSITORY_REQUIRED` permanece apenas como marcador documental/validador; nenhum endpoint novo deve retornar dados fake.
- Gaps restantes: validação em ambiente com SDK .NET e implementação futura de PDF binário real (`pdf_pending` quando aplicável).

## Gap monitorado — importação de legado

A sprint adiciona endpoints administrativos `/migration/*` com dry-run, apply controlado, conciliação, rollback e cutover readiness. Gaps restantes: homologação com base real, tuning de performance em lotes grandes, política final de retenção de snapshots e automação de cutover produtivo (não executada por design).
