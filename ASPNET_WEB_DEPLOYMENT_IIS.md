# Deploy IIS Valora.Web

1. Instale .NET Hosting Bundle 8.
2. Execute `tools\windows\web\05-publicar-web-iis-dry-run.bat`.
3. Configure `Api__BaseUrl` como variável do App Pool/site.
4. Publique a pasta gerada pelo `dotnet publish`.
5. Valide `/health` da API e navegação Web.

## Diagnóstico Sprint 41
- Valora.Web oficial: `backend/Valora.Web`, ASP.NET Core MVC net8.0, Bootstrap 5, JavaScript puro, jQuery e AJAX.
- Solution oficial: `backend/Valora.sln`.
- Integração: Valora.Web consome Valora.Api via HTTP configurável por `Api:BaseUrl`/`Api__BaseUrl`.
- Proibições mantidas: sem acesso direto a banco, repositories, Dapper, EF, Valora.Infrastructure ou Firebase no Valora.Web.
- Front legado preservado; produção continua com `DATA_PROVIDER: 'firebase'` e `ALLOW_API_PRODUCTION_CUTOVER: false` até cutover aprovado.
- Gaps permitidos somente com fallback controlado documentado e `data-gap-controlled="true"`.
