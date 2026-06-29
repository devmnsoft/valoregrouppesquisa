# Valora.Web Validation Report

Gates adicionados: web:aspnet-only, web:no-data-access, web:no-generic-official-render, web:specific-renderers, web:production-gaps, web:no-sensitive-ui, web:iis-readiness, web:docker-runtime e web:final-release-gate.

## Diagnóstico Sprint 41
- Valora.Web oficial: `backend/Valora.Web`, ASP.NET Core MVC net8.0, Bootstrap 5, JavaScript puro, jQuery e AJAX.
- Solution oficial: `backend/Valora.sln`.
- Integração: Valora.Web consome Valora.Api via HTTP configurável por `Api:BaseUrl`/`Api__BaseUrl`.
- Proibições mantidas: sem acesso direto a banco, repositories, Dapper, EF, Valora.Infrastructure ou Firebase no Valora.Web.
- Front legado preservado; produção continua com `DATA_PROVIDER: 'firebase'` e `ALLOW_API_PRODUCTION_CUTOVER: false` até cutover aprovado.
- Gaps permitidos somente com fallback controlado documentado e `data-gap-controlled="true"`.
