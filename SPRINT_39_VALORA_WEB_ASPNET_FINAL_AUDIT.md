# Sprint 39 — Auditoria final Valora.Web ASP.NET

Data: 2026-06-29.

## Escopo auditado
- `backend/Valora.Web/`
- `backend/Valora.Api/`
- `backend/Valora.Application/`
- `backend/Valora.Infrastructure/`
- `database/postgresql/`
- `package.json`
- `scripts/`
- `tests/e2e-web/`
- `docker-compose.yml`
- `tools/windows/`

## Respostas objetivas
1. **O Valora.Web continua sendo ASP.NET Core?** Sim. `backend/Valora.Web/Valora.Web.csproj` usa `Microsoft.NET.Sdk.Web` e `net8.0`.
2. **O Valora.Web está na solution?** Sim. `backend/Valora.sln` contém o projeto `Valora.Web`.
3. **Existe algum front oficial paralelo indevido?** Não foi identificado `frontend-web/server.js` como front oficial. Node permanece restrito a scripts, validações e Playwright.
4. **Quais controllers MVC ainda não têm `ILogger<T>`?** Nenhum entre os controllers auditados.
5. **Quais controllers MVC ainda não têm try/catch pertinente?** Nenhum entre os controllers auditados; as actions principais renderizam views dentro de try/catch com `LogError`.
6. **Quais views ainda estão genéricas?** Nenhuma view administrativa principal depende somente de tela genérica; ainda há estrutura visual comum intencional para consistência Bootstrap.
7. **Quais telas ainda usam fallback genérico demais?** Fallbacks existem apenas como gap controlado com `data-gap-controlled="true"`.
8. **Quais telas ainda usam `ValoraAdminPage` como base principal?** Nenhuma tela oficial auditada.
9. **Quais módulos administrativos estão prontos?** Dashboard, Organization, Users, Forms, Surveys, PublicLinks, Responses, Communications, Audit, Migration, Settings, Plans e EnvironmentStatus têm view própria e JS próprio.
10. **Quais módulos administrativos ainda são gap?** Configurações avançadas de senha/notificações e certificado binário completo permanecem como gaps não bloqueantes documentados.
11. **Quais endpoints da API existem e são consumidos?** `/me`, `/health`, `/health/database`, `/organization/current`, `/organization/current/usage`, `/organization/current/limits`, `/users`, `/forms`, `/surveys`, `/surveys/{surveyId}/links`, `/survey-links/{linkId}/status`, `/responses`, `/responses/{responseId}`, `/responses/{responseId}/result`, `/audit/events`, `/settings`, além dos endpoints de comunicação/migração/planos conforme módulos existentes.
12. **Quais endpoints da API faltam?** `/settings/password` e recursos administrativos avançados de notificação permanecem não bloqueantes.
13. **Quais endpoints bloqueiam produção?** Nenhum bloqueante conhecido para a homologação do front ASP.NET oficial; os endpoints mínimos internos existem ou têm fallback controlado.
14. **Quais endpoints podem ficar para próxima sprint com fallback controlado?** `/settings/password`, notificações avançadas, certificado binário completo e automações comerciais avançadas.
15. **Quais telas ainda podem exibir JSON bruto?** Nenhuma tela administrativa oficial deve exibir JSON bruto; validadores bloqueiam dumps conhecidos.
16. **Quais telas ainda podem vazar token, resultToken ou hash?** Nenhuma tela oficial deveria vazar token/hash; validadores de segurança e sanitização continuam obrigatórios.
17. **Quais telas ainda podem quebrar no mobile?** Nenhuma quebra bloqueante conhecida; as tabelas usam `.table-responsive` e há specs mobile Playwright.
18. **Quais riscos impedem homologação final?** Dependência de ambiente live para E2E completo, autenticação real para endpoints `[Authorize]`, e gaps não bloqueantes documentados. Não há bloqueio estrutural do Valora.Web ASP.NET como front oficial.

## Mapeamento de termos críticos
- `Valora.Web`: presente como projeto ASP.NET Core oficial.
- `frontend-web`: não deve existir como front oficial; gate valida ausência de `frontend-web/server.js`.
- `server.js`: permitido apenas fora do papel de front oficial.
- `React`, `Vue`, `Angular`: não utilizados no `Valora.Web` oficial.
- `ValoraAdminPage`: não é base principal dos módulos oficiais.
- `em ativação`: permitido somente com `data-gap-controlled="true"`.
- `data-gap-controlled`: usado para fallback controlado.
- `JSON.stringify` e `<pre`: bloqueados quando usados como dump bruto nas telas oficiais.
- `console.log`: proibido para payload sensível.
- `placeholder`, `TODO`, `mock`, `stub`, `pending`, `NotImplemented`: devem permanecer fora das telas oficiais ou documentados fora do release.
- `Executar ação`, `Tela Bootstrap API-first`, `Nenhum dado carregado ainda`: termos genéricos bloqueados pelos gates aplicáveis.
- `password_hash`, `token_hash`, `result_token_hash`: não devem ser retornados nem exibidos pelo front ASP.NET.

## Diagnóstico final
O `backend/Valora.Web` está consolidado como front oficial ASP.NET Core MVC do SaaS, consumindo `Valora.Api` via HTTP/AJAX, sem acesso direto a banco, repositories, Dapper, Entity Framework ou Firebase. A homologação final depende da execução dos gates em ambiente com API, banco e credenciais de teste disponíveis.
