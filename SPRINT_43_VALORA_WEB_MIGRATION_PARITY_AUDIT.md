# Sprint 43 — Auditoria de Paridade Valora.Web

## Diagnóstico real
O front oficial permanece em `backend/Valora.Web` com ASP.NET Core MVC/Razor, Bootstrap 5, JavaScript puro, jQuery e AJAX. O front legado da raiz foi preservado para referência de cutover.

## Telas do front antigo e jornadas
1. Pública: home, planos/LGPD, pesquisa pública por link, envio de resposta, resultado público por token e certificado.
2. Empresa: login, dashboard, organização, usuários, formulários, pesquisas, links públicos, respostas, relatórios, certificados, comunicações, configurações e status.
3. Valora admin: clientes/empresas, planos, módulos, financeiro, logs, backup, suporte global e configurações globais.
4. Participante: responder pesquisa, consultar resultado permitido, certificado permitido e privacidade/LGPD.

## Permissões mapeadas
`ROLE_DEFINITIONS`, `canAccessPortal`, `canManageCompanies`, `canManagePlans`, `canManageModules`, `canManageGlobalSettings`, `canManageCompanyUsers`, `canCreateForms`, `canCreateSurveys`, `canSendInvites`, `canViewResponses`, `canViewReports`, `canViewFinance`, `canViewLogs`, `canBackup`, `canAnswerSurveys`, `canHandleSupport`, `canHandleGlobalSupport` e `restrictedToDepartment` foram migradas para `backend/Valora.Web/wwwroot/js/security/role-definitions.js`.

## Módulos mapeados
`MODULE_DEFINITIONS` e `ValoraModules` foram migrados com `clientes`, `financeiro`, `planos`, `modulos`, `usuarios`, `funcionarios`, `formularios`, `pesquisas`, `convitesEmail`, `respostas`, `relatorios`, `certificados`, `actionPlans`, `valorabot`, `support`, `lgpd`, `integrations`, `exportacoes`, `benchmark`, `whiteLabel`, `backup` e `logs`.

## Regras migradas
`ValoraWebPermissions` implementa `canUseModule`, `canAccessCompany`, `resolveResponsePermissions`, `canViewResponse`, `canEditResponse` e `canDownloadResponseCertificate` sem Firebase e sem acesso direto a banco.

## API e dados administrativos
O conflito Swagger foi removido: o endpoint oficial de resultado é `GET /responses/{responseId}/result` em `ResponsesController`. O endpoint legacy foi movido para `/legacy/responses/{responseId}/result` e ocultado do Swagger. O `WebAdminModulesController` não retorna mais dados fixos proibidos; endpoints sem repository real retornam gap controlado `WEB_ADMIN_REAL_REPOSITORY_REQUIRED`.

## Endpoints que ainda precisam de repository real
Organization, Users, Forms, SurveysAdmin, SurveyLinks, ResponsesAdmin, Audit, Settings e Usage ainda precisam ser concluídos com repositories reais para produção plena.

## Fallbacks controlados
Os módulos administrativos listados acima dependem de fallback controlado documentado em `ASPNET_WEB_API_GAPS.md`.

## Gaps bloqueantes resolvidos
- Conflito Swagger de `GET /responses/{responseId}/result`.
- Raiz da API `/` agora retorna JSON seguro.
- Dados fake administrativos proibidos foram removidos.

## Gaps para próxima sprint
- Completar Application Services e Infrastructure repositories reais para todos os módulos administrativos.
- Expandir testes Playwright com fixtures autenticadas reais.
- Validar cutover em ambiente homologado com dados migrados.

## O que falta para substituir o front antigo
Concluir repositories reais administrativos, homologar jornada completa com usuários reais, executar gates live e aprovar cutover/rollback.
