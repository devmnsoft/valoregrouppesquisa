# Auditoria Sprint 38 — Valora.Web oficial

1. O Valora.Web está 100% ASP.NET? Sim: `backend/Valora.Web/Valora.Web.csproj` usa `Microsoft.NET.Sdk.Web` e `net8.0`.
2. Existe algum frontend web paralelo indevido? Não foi criado front Node oficial; legado permanece preservado.
3. Quais controllers MVC ainda não têm ILogger<T>? Nenhum controller MVC listado permanece sem `ILogger<T>`.
4. Quais controllers MVC ainda são genéricos demais? Nenhum dos módulos principais depende apenas de controller genérico; actions específicas foram preservadas.
5. Quais views ainda usam layout genérico? As views usam layout compartilhado MVC, mas conteúdo de módulo é específico.
6. Quais telas ainda usam componente genérico para tabela? Nenhuma tela principal usa `ValoraAdminPage.init`.
7. Quais telas ainda mostram "em ativação" sem gap controlado? Nenhuma; o validador exige `data-gap-controlled="true"`.
8. Quais telas ainda não têm UX própria? Nenhuma entre Dashboard, Organização, Usuários, Formulários, Pesquisas, Links públicos, Respostas, Comunicações, Auditoria, Migração, Ambiente, Configurações e Planos.
9. Quais módulos autenticados ainda não têm endpoint real? Configurações avançadas de senha/notificações têm gap não bloqueante.
10. Quais módulos autenticados ainda não têm fallback documentado? Nenhum; ver `ASPNET_WEB_API_GAPS.md`.
11. Quais módulos ainda não têm Playwright E2E? Foram adicionadas specs oficiais por módulo; execução depende do ambiente local.
12. Quais endpoints precisam ser criados na API? Endpoints mínimos já existem em `WebAdminModulesController`; senha avançada fica para sprint futura.
13. Quais endpoints faltantes bloqueiam produção? Nenhum conhecido.
14. Quais endpoints faltantes podem ficar para sprint futura? `/settings/password` e notificações avançadas.
15. Quais riscos ainda impedem homologação final? Necessário executar suíte completa com API, PostgreSQL, Playwright e ambiente Docker/IIS reais.

## Mapeamento de termos
`ValoraAdminPage`, `JSON.stringify`, `<pre`, `console.log`, `placeholder`, `TODO`, `mock`, `stub`, `pending`, `NotImplemented`, `Executar ação`, `Tela Bootstrap API-first` e `Nenhum dado carregado ainda` são monitorados por validadores e checklist.
