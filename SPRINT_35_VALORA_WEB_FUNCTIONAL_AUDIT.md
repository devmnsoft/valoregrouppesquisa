# Sprint 35 — Auditoria funcional do Valora.Web

Data: 2026-06-28.

## Escopo auditado

- `backend/Valora.sln`
- `backend/Valora.Web/`
- `backend/Valora.Api/`
- `backend/Valora.Application/`
- `backend/Valora.Infrastructure/`
- `package.json`
- `docker-compose.yml`
- `scripts/`
- `tests/`
- `tools/windows/`

## Respostas objetivas

1. **Valora.Web existe?** Sim. O projeto está em `backend/Valora.Web/Valora.Web.csproj`.
2. **Valora.Web está na solution?** Sim, validado por `scripts/validate-aspnet-web-project.js`.
3. **Valora.Web compila?** O build não pôde ser executado neste container porque o comando `dotnet` não está instalado. Os validadores estáticos do projeto passam.
4. **Quais controllers MVC existem?** `HomeController`, `AccountController`, `DashboardController`, `PlansController`, `OrganizationController`, `UsersController`, `FormsController`, `SurveysController`, `PublicSurveyController`, `ResultsController`, `CertificatesController`, `CommunicationsController`, `AuditController`, `MigrationController`, `EnvironmentStatusController` e `SettingsController`.
5. **Quais views existem?** Views de conta, dashboard, planos, organização, usuários, formulários, pesquisas, links públicos, pesquisa pública, resultado, certificado, comunicações, auditoria, migração, status do ambiente, configurações e parciais de layout.
6. **Quais arquivos JS existem?** Client AJAX, APIs JS para auth/plans/surveys/public-survey/results/certificates/communications/audit/migration/health, core de sessão/guards/loading/toast e scripts de página.
7. **Quais telas ainda são placeholder?** As telas administrativas sem endpoint específico ainda usam chamadas seguras de health/listagem como fallback operacional até a API expor CRUD dedicado.
8. **Quais telas já chamam API real?** Login, cadastro, forgot/reset password, dashboard/status, planos, pesquisa pública, resultado, certificado, comunicações/e-mails e migração.
9. **Quais telas ainda não têm jQuery AJAX?** Nenhuma tela funcional depende de `fetch`; o tráfego API passa por `AjaxClient` com `$.ajax`.
10. **Quais endpoints da API ainda faltam?** CRUD completo de organização, usuários, formulários, pesquisas e auditoria paginada devem permanecer registrados em `ASPNET_WEB_API_GAPS.md` quando não expostos pela API.
11. **Quais fluxos ainda não estão testados?** E2E live completo depende de ambiente local com API e banco; specs estáticas/contratuais foram adicionadas para forgot/reset e status ambiente.
12. **Quais validações ainda são só por string?** Validações client-side de campos obrigatórios/e-mail/senha são leves e devem ser complementadas por validação tipada na API.
13. **Quais pontos podem quebrar em produção?** API offline, CORS mal configurado, rota IIS/proxy retornando HTML, endpoints admin ainda ausentes e falta do runtime .NET no ambiente.
14. **Quais ajustes faltam para homologar o front SaaS?** Executar `web:build`/`web:e2e` em host com .NET e Playwright, validar API live, concluir gaps de CRUD admin e aprovar cutover sem alterar Firebase em produção.

## Resultado da intervenção

- Client AJAX foi reforçado com métodos HTTP completos, binário seguro, sessão em `sessionStorage`, `X-Correlation-Id`, timeout e normalização de erros sem stack trace.
- Guards de sessão foram ativados para rotas autenticadas e logout.
- APIs JS de resultado/certificado usam download binário via `requestBinary`.
- Validador `web:functional-flow` foi criado e adicionado ao `package.json`.
- Specs Playwright adicionais cobrem forgot/reset e status ambiente sem gerar screenshots.
- Nenhum arquivo binário foi criado.
