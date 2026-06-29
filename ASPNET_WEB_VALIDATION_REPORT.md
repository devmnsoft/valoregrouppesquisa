# Sprint 38 — Valora.Web ASP.NET oficial

## Diagnóstico
O front oficial é `backend/Valora.Web`, ASP.NET Core MVC net8.0 com Bootstrap, JavaScript, jQuery e consumo HTTP da Valora.Api via `AjaxClient`. Não há acesso direto a banco, Dapper, EF, repositories ou Firebase no projeto web. O frontend legado foi preservado e produção continua com `DATA_PROVIDER: firebase` e `ALLOW_API_PRODUCTION_CUTOVER: false`.

## Módulos oficializados
Dashboard, Organização, Usuários, Formulários, Pesquisas, Links públicos, Respostas, Comunicações, Auditoria, Migração, Ambiente, Configurações e Planos possuem view MVC e JavaScript específico por página.

## Endpoints consumidos
`GET /me`, `/health`, `/health/database`, `/organization/current`, `/organization/current/usage`, `/organization/current/limits`, `/users`, `/forms`, `/surveys`, `/surveys/{surveyId}/links`, `/responses`, `/audit/events`, `/settings`, `/admin/email-jobs`, `/admin/migration/status` e `/plans/public`.

## Segurança e operação
Gaps são apresentados somente com `data-gap-controlled="true"`; erros exibem mensagem segura e correlationId quando fornecido pela API. Não há arquivos binários novos.

## Documento
Este arquivo registra o artefato `ASPNET_WEB_VALIDATION_REPORT.md` da industrialização Sprint 38.
