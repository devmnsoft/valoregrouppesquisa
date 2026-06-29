# Sprint 41 — Auditoria Final Valora.Web ASP.NET

## Respostas objetivas
1. Sim, o Valora.Web continua ASP.NET Core MVC.
2. Sim, está em `backend/Valora.sln`.
3. Não há front oficial paralelo aprovado; Node fica restrito a scripts/Playwright.
4. O Web não deve acessar banco/Dapper/EF/repositories/Firebase; validadores bloqueiam isso.
5. Controllers mapeados possuem ILogger e try/catch nas actions principais.
6. Páginas oficiais não devem usar render genérico como render principal.
7. `ValoraOfficialPage`/`ValoraAdminPage` ficam proibidos como base principal.
8. Módulos com JS próprio: dashboard, organization, users, forms, surveys, public-links, responses, communications, audit, migration, environment-status, settings e plans.
9. Consumo real via APIs JS de módulo para auth, plans, health e endpoints administrativos.
10. Fallbacks restantes são controlados e documentados em `ASPNET_WEB_API_GAPS.md`.
11. Bloqueantes: nenhum sem endpoint, justificativa ou fallback controlado.
12. Não bloqueantes: melhorias operacionais e persistência real mais profunda em alguns endpoints mínimos.
13. Endpoints internos faltantes bloqueantes: nenhum na lista mínima da Sprint 41.
14. Endpoints criados agora: organização, usuários, forms, surveys, links, responses, audit, settings, usage e limits.
15. Sprint futura: analytics avançado, billing detalhado e filtros históricos profundos.
16. Telas oficiais não podem exibir JSON bruto.
17. Validadores bloqueiam token/hash/segredo/stack trace na UI.
18. Mobile validado por CSS responsivo e specs E2E.
19. Docker publica API e Web com portas 5080/5088 e Postgres 5434.
20. IIS possui web.config, guia e scripts Windows.
21. Release candidate final depende de execução integral dos gates em ambiente com Docker/.NET/Playwright.

## Mapeamento de termos auditados
Termos genéricos/sensíveis auditados: ValoraOfficialPage, ValoraAdminPage, renderRows, function rows, Registro 1, Item 1, em ativação fora de `data-gap-controlled`, JSON.stringify, `<pre`, console.log, placeholder, TODO, mock, stub, pending, NotImplemented, Executar ação, Tela Bootstrap API-first, Nenhum dado carregado ainda, password_hash, token_hash, result_token_hash, connection string, Dapper, Npgsql, DbConnection, Valora.Infrastructure e Firebase.

## Diagnóstico Sprint 41
- Valora.Web oficial: `backend/Valora.Web`, ASP.NET Core MVC net8.0, Bootstrap 5, JavaScript puro, jQuery e AJAX.
- Solution oficial: `backend/Valora.sln`.
- Integração: Valora.Web consome Valora.Api via HTTP configurável por `Api:BaseUrl`/`Api__BaseUrl`.
- Proibições mantidas: sem acesso direto a banco, repositories, Dapper, EF, Valora.Infrastructure ou Firebase no Valora.Web.
- Front legado preservado; produção continua com `DATA_PROVIDER: 'firebase'` e `ALLOW_API_PRODUCTION_CUTOVER: false` até cutover aprovado.
- Gaps permitidos somente com fallback controlado documentado e `data-gap-controlled="true"`.
