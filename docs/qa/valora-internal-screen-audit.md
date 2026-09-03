# Valora Insight™ — mapa interno de QA

**Revisão:** 2026-09-03  
**Escopo:** experiência autenticada, rotas executivas e estados de erro do `Valora.Web`.  
**Legenda:** ✅ confirmado por inspeção automatizada/estática; 🧪 requer execução integrada com API e banco; ➖ não aplicável; ⚠️ débito identificado.

> Este documento não transforma inspeção de código em evidência de produção. A coluna **Runtime** só pode receber ✅ depois de login real, refresh, chamadas BFF e navegação serem exercitados no ambiente publicado. Em telas dependentes de tenant, Super Administradores sem contexto devem receber o seletor/banner de organização, nunca um tenant implícito.

## Critérios transversais

- Autenticação por cookie HTTP-only; tokens permanecem no BFF. Refresh transitório da API não apaga a sessão local e respostas de feature `401` só redirecionam quando trazem código explícito de sessão inválida.
- O shell autenticado fornece navegação, cabeçalho, manual curto contextual, banner de organização, toast host e modal de confirmação.
- A página deve apresentar uma ação ou próximo passo real; estados sem dados precisam orientar o usuário sem fabricar indicadores.
- Erros ao usuário são sanitizados e incluem, quando disponível, apenas a referência de correlação — nunca SQL, classe, exception ou stack trace.
- Alterações estruturais publicadas preservam versão e rastreabilidade. Evidência e recomendação apoiam, mas não substituem, decisão humana.

## Matriz de telas mínimas

| Tela / rota canônica | Rota | Auth | Organização | Título + manual | Ação / função visível | Estado vazio + feedback | Responsivo / visual | Runtime | Resultado / próximo passo |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|
| Dashboard `/Dashboard` | ✅ | ✅ | 🧪 | ✅ | ✅ cards, alertas e atalhos | ✅ | ✅ | 🧪 | Validar métricas com tenant real. |
| Forms `/Forms` | ✅ | ✅ | 🧪 | ✅ | ✅ busca, filtros, criar e editar | ✅ | ✅ | 🧪 | Biblioteca pronta para smoke integrado. |
| Forms/Create `/Forms/Create` | ✅ | ✅ | 🧪 | ✅ | ✅ abre criação tipada no workspace | ✅ | ✅ | 🧪 | Confirmar persistência e validações da API. |
| Forms/Builder `/Forms/{id}/Builder` | ✅ | ✅ | ✅ | ✅ | ✅ editar, ordenar, publicar | ✅ | ✅ | 🧪 | Testar conflito de versão e imutabilidade. |
| Forms/Preview `/Forms/{id}/Preview` | ✅ | ✅ | ✅ | ✅ | ✅ abre preview desktop/mobile | ✅ | ✅ | 🧪 | Testar formulário vazio e publicado. |
| Diagnostics `/Diagnostics` | ✅ | ✅ | ✅ | ✅ | ✅ workspace e progresso | ✅ | ✅ | 🧪 | Exercitar abertura e encerramento da coleta. |
| Surveys `/Surveys` | ✅ | ✅ | ✅ | ✅ | ✅ criar, convidar e acompanhar | ✅ | ✅ | 🧪 | Validar convite, prazo e LGPD. |
| Results `/Results` | ✅ | ✅ | ✅ | ✅ | ✅ leitura e acessos executivos | ✅ | ✅ | 🧪 | Validar evidências e baixa confiança. |
| Reports `/Reports` | ✅ | ✅ | ✅ | ✅ | ✅ gerar, histórico e preview | ✅ | ✅ | 🧪 | Exercitar download e processamento. |
| Certificates `/Certificates` | ✅ | ✅ | ✅ | ✅ | ✅ emissão, consulta e validação | ✅ | ✅ | 🧪 | Confirmar download e trilha de auditoria. |
| ActionCenter `/ActionCenter` | ✅ | ✅ | ✅ | ✅ | ✅ planos, prioridades e responsáveis | ✅ | ✅ | 🧪 | Testar vínculo com evidência. |
| Evolution `/Evolution` | ✅ | ✅ | ✅ | ✅ | ✅ ciclos, snapshots e comparação | ✅ | ✅ | 🧪 | Validar períodos comparáveis. |
| Journey `/Journey` | ✅ | ✅ | ✅ | ✅ | ✅ timeline e detalhes | ✅ | ✅ | 🧪 | Confirmar filtros e origem dos eventos. |
| DecisionCenter `/DecisionCenter` | ✅ | ✅ | ✅ | ✅ | ✅ alertas e decisões | ✅ | ✅ | 🧪 | Confirmar decisão humana e acknowledge. |
| Indicators `/Indicators` | ✅ | ✅ | ✅ | ✅ | ✅ catálogo, metas e medições | ✅ | ✅ | 🧪 | Testar fontes, unidade e período. |
| Benchmarks `/Benchmarks` | ✅ | ✅ | ✅ | ✅ | ✅ coortes, comparação e privacidade | ✅ | ✅ | 🧪 | Confirmar limiar mínimo de anonimização. |
| Methodology `/Methodology` | ✅ | ✅ | 🧪 | ✅ | ✅ versões e publicação | ✅ | ✅ | 🧪 | Exercitar perfil `admin_valora`. |
| Governance `/Governance` | ✅ | ✅ | ✅ | ✅ | ✅ ciclos, reuniões e decisões | ✅ | ✅ | 🧪 | Validar antiforgery e evidência. |
| DataHub `/DataHub` | ✅ | ✅ | ✅ | ✅ | ✅ catálogo e integrações | ✅ | ✅ | 🧪 | Smoke das fontes autorizadas. |
| SuccessCenter `/SuccessCenter` | ✅ | ✅ | 🧪 | ✅ | ✅ saúde, risco e próximos passos | ✅ | ✅ | 🧪 | Validar visão global versus tenant. |
| SecurityCompliance `/SecurityCompliance` | ✅ | ✅ | 🧪 | ✅ | ✅ controles e evidências | ✅ | ✅ | 🧪 | Testar menor privilégio. |
| Plans `/Plans` | ✅ | 🧪 | ➖ | ✅ | ✅ comparação e contratação | ✅ | ✅ | 🧪 | Verificar jornada autenticada e pública. |
| Administration `/Administration` | ✅ | ✅ | ➖ | ✅ | ✅ hub global completo | ✅ | ✅ | 🧪 | Obrigatório perfil `admin_valora`. |
| Users `/Users` | ✅ | ✅ | 🧪 | ✅ | ✅ filtros, convite e perfis | ✅ | ✅ | 🧪 | Validar escopo e sessões. |
| Organizations `/Administration/Organizations` | ✅ | ✅ | ➖ | ✅ | ✅ clientes e seleção explícita | ✅ | ✅ | 🧪 | Confirmar que nenhum tenant é escolhido implicitamente. |
| Login `/Account/Login` | ✅ | ➖ | ➖ | ✅ | ✅ entrar e recuperar acesso | ✅ | ✅ | 🧪 | Testar credencial válida/inválida e “lembrar”. |
| Erro 403 `/error/403` | ✅ | ➖ | ➖ | ✅ | ✅ retorno seguro | ✅ | ✅ | 🧪 | Deve preservar status HTTP 403. |
| Erro 404 `/error/404` | ✅ | ➖ | ➖ | ✅ | ✅ retorno seguro | ✅ | ✅ | 🧪 | Deve preservar status HTTP 404. |
| Erro 500 `/error/500` | ✅ | ➖ | ➖ | ✅ | ✅ referência segura | ✅ | ✅ | 🧪 | Deve preservar status HTTP 500 sem stack trace. |

## Roteiro de smoke obrigatório

1. Iniciar API e Web com banco de homologação isolado; abrir desktop (1440 × 900) e mobile (390 × 844).
2. Entrar como usuário comum, atualizar a página três vezes e percorrer Dashboard → Forms → Surveys → Results → Reports.
3. Deixar uma chamada de feature retornar `401` sem código de sessão: confirmar mensagem local, cookie preservado e ausência de redirect.
4. Invalidar de fato refresh token/sessão: confirmar `SESSION_EXPIRED`, toast claro e redirect com `returnUrl` local.
5. Entrar como `admin_valora` sem organização: confirmar menu global completo e banner/seletor; páginas globais carregam e páginas de tenant não consultam `Guid.Empty`.
6. Selecionar explicitamente uma organização autorizada e repetir Dashboard, diagnóstico, resultados e administração; limpar seleção e confirmar ausência de vazamento entre tenants.
7. Criar rascunho em `/Forms/Create`, editar no Builder, abrir `/Preview`, publicar e confirmar bloqueio estrutural com orientação para nova versão.
8. Conferir teclado, foco, toast/modal, loading de submit, tabelas sem overflow e textos sem corte nas duas larguras.

## Evidência pendente por limitação do ambiente desta revisão

O SDK `dotnet` não está instalado no container de revisão. Por isso build, testes, execução integrada e capturas de navegador permanecem **🧪**, e não foram indevidamente marcados como aprovados. Execute no agente/CI com .NET 10:

```bash
cd backend
dotnet clean Valora.sln
dotnet restore Valora.sln
dotnet build Valora.sln --no-restore
dotnet test Valora.sln --no-build
```
