# Sprint 37.1 — Auditoria da Área Autenticada Valora.Web

## Escopo auditado
- backend/Valora.Web/
- backend/Valora.Api/Controllers/
- backend/Valora.Application/
- backend/Valora.Infrastructure/
- database/postgresql/
- package.json
- scripts/
- tests/e2e-web/
- docker-compose.yml
- tools/windows/

## Respostas objetivas
1. Módulos autenticados existentes: Dashboard, Plans, Organization, Users, Forms, Surveys, PublicLinks, Responses, Communications, Audit, Migration, EnvironmentStatus e Settings.
2. Telas genéricas restantes: nenhuma bloqueante; estados vazios e indisponibilidade usam componentes controlados.
3. Telas com “módulo em ativação”: apenas fallback controlado renderizado por JavaScript quando endpoint falha.
4. Telas sem chamada real de API: nenhuma das telas administrativas principais; gaps externos estão documentados.
5. JS sem AjaxClient: páginas públicas específicas usam seus clientes próprios; APIs administrativas usam AjaxClient.
6. Endpoints administrativos existentes: /organization/current, /users, /forms, /surveys, /survey-links, /responses, /audit/events, /settings, /organization/current/usage e /organization/current/limits.
7. Faltam endpoints avançados para senha, notificações granulares, fila SMTP completa e relatórios analíticos.
8. Criados nesta sprint: endpoints mínimos do painel SaaS no WebAdminModulesController.
9. Podem ficar como gap: senha, notificações avançadas, exportações, analytics e processamento completo de fila.
10. Bloqueiam produção: persistência real multi-tenant completa para CRUD administrativo e auditoria transacional.
11. Homologáveis com fallback: comunicações, migração, ambiente, certificados e relatórios avançados.
12. Rotas autenticadas sem token: nenhuma rota administrativa no front deixa de chamar Guards.requireAuth.
13. Páginas com risco de JSON bruto: mitigado pelos validadores web:no-json-dump e web:admin-security.
14. Risco de token/resultToken: mitigado por sessionStorage para auth e bloqueio de resultToken em localStorage.
15. Pontos móveis: tabelas administrativas usam table-responsive; menus exigem validação Playwright mobile.
16. Gates: web:authenticated-modules, web:crud-api-gaps, web:auth-guards, web:no-json-dump, web:admin-ux, web:api-endpoints e web:admin-security.

## Mapa de termos críticos
Dashboard, Organization, Users, Forms, Surveys, PublicLinks, Responses, Communications, Audit, Migration, EnvironmentStatus, Settings e Plans foram mapeados. Termos como JSON.stringify, <pre, console.log, placeholder, TODO, mock, stub, pending, NotImplemented e PLAN_LIMIT_REACHED ficam cobertos por validações e documentação de gaps.
