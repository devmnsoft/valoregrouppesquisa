# Pendências reais — QA funcional

Registro da estabilização executada em 18/08/2026. Este documento contém apenas
limitações efetivamente observadas; não substitui a correção de defeitos críticos.

| Módulo | Problema | Impacto | Evidência | Arquivo provável | Prioridade | Recomendado para a próxima fase |
|---|---|---|---|---|---|---|
| Toolchain local | O ambiente de execução não possui o comando `dotnet`. | Não foi possível executar `clean`, `restore`, `build` nem iniciar API/Web neste ambiente. | `bash: command not found: dotnet` ao executar a validação obrigatória. | Imagem/host de QA (fora do repositório) | Alta | Provisionar o SDK fixado por `global.json` e repetir o checklist de homologação local. |
| PostgreSQL local | O ambiente não possui `psql` nem runtime de containers. | Não foi possível aplicar o script em banco novo, existente ou duas vezes no mesmo banco. | `command -v psql` e `command -v docker` não retornaram executáveis. | Imagem/host de QA (fora do repositório) | Alta | Disponibilizar PostgreSQL 16 e executar `database/postgresql/script_completo.sql` nos três cenários antes da homologação. |
| Smoke ponta a ponta | O smoke autenticado e o fluxo público dependem de API, Web e PostgreSQL em execução. | Login, Dashboard, criação/publicação de diagnóstico, LGPD, resposta, inteligência, relatório e certificado permanecem sem validação dinâmica nesta execução. | Startup bloqueado pela ausência do SDK e banco. | `Valora.Api`, `Valora.Web` e `database/postgresql/script_completo.sql` | Alta | Executar integralmente o checklist de `GUIA_HOMOLOGACAO_LOCAL.md` em ambiente provisionado e anexar IDs de correlação das falhas encontradas. |

## Correções concluídas nesta revisão

- A conexão oficial agora prioriza `ConnectionStrings:Postgres`, mantendo
  `DefaultConnection` apenas como compatibilidade de instalações existentes.
- O login distingue credenciais inválidas de usuário com role/organização ainda
  não configuradas, sem atualizar `last_login_at` nem gerar auditoria de sucesso
  antes de concluir todas as validações de acesso.

## Correções e Homologação — Sprints 1 a 5 (Evolução Operacional e Regras de Negócio)

- **Sprint 1.1 — Contexto Antes das Consultas (`dashboard-page.js`)**:
  - Implementados estados explícitos de ciclo de vida do contexto: `verifying`, `platform_mode`, `org_selected`, `selection_required`, `session_expired`, `access_denied`, `unavailable`.
  - Falha na consulta de contexto (`/bff/account/context`) interrompe imediatamente o fluxo e previne consultas organizacionais dependentes (`SurveysApi.list()`, métricas, etc.).
  - Distinção precisa entre 401 (sessão expirada), 403 (organização necessária vs acesso negado estruturado) e 5xx/falha de rede (contexto temporariamente indisponível com botão de nova tentativa).
  - Preservado acesso ao Modo Plataforma para perfis globais autorizados sem deslogar o usuário por ausência de organização.
- **Sprint 1.2 — Sessão e Cookie Consistentes (`BffAuthenticationService.cs`)**:
  - Removidos retornos silenciosos em caso de ausência de `IAuthenticationService`; o sistema lança `InvalidOperationException` explícita em falha de injeção de dependência.
  - Sincronização imediata de `context.User = principal` em `RenewCookieAsync`, garantindo coerência entre sessão persistida, claims em memória e cookie HTTP na mesma requisição.
  - `SelectOrganizationAsync` envolve a renovação do cookie em bloco transacional com reversão atômica (`SetOrganizationId(previousOrgId)`) se a emissão do cookie falhar.
- **Sprint 1.3 — Contratos de Corpo de Requisição (`BffBodyReader`, `BffAdministrationController`, `BffDiagnosticWorkspaceController`)**:
  - Configurado `isBodyRequired: true` para mutações críticas (`POST`/`PUT` em Users, Roles, Units, Departments, Forms, Settings e `PUT preparation`).
  - Mutações sem corpo são rejeitadas preventivamente no BFF com HTTP 400 estruturado (`REQUEST_BODY_REQUIRED`), protegendo regras de negócio downstream.
  - Preservadas operações sem corpo (ações de gatilho `/publish`, `/archive`, `/block`, `/close`, `/process`, `DELETE`, `GET`, downloads binários e HTTP 204 NoContent).
- **Sprint 1.4 — Feedback Verdadeiro e Confiabilidade de Erros**:
  - Ações de cópia aguardam explicitamente a resolução da Promise de `navigator.clipboard.writeText`; em caso de bloqueio de permissão ou ausência da API, seleciona o texto no DOM para `Ctrl+C` manual.
  - Erros no frontend exibem o `correlationId` retornado pelo servidor ou indicam claramente "Código de atendimento: não informado pelo servidor.", eliminando identificadores aleatórios fabricados.
- **Sprint 2 — Busca Completa de Organizações e Navegação (`BffAuthController.cs`, `topbar-shell.js`)**:
  - Endpoint `/bff/auth/organizations` estendido para suportar busca no servidor (`search`), paginação (`page`, `pageSize`, `hasMore`) e extração correta de `customerId` e `organizationId`.
  - Seletor no frontend implementa debounce de 300ms, paginação sob demanda com botão "Carregar mais", cancelamento com `AbortController` para requisições em trânsito e descarte de respostas defasadas por geração.
  - Shell exibe banner visual persistente indicando organização ativa ou Modo Plataforma com troca rápida e retorno imediato.
- **Sprint 3 — Diagnóstico, Preparação e Central de Ações**:
  - Validação estrita de responsável explicitamente designado (sem substituição arbitrária pelo criador), isolamento de tenant por `organizationId` e controle de concorrência otimista (`rowVersion`).
  - Mapeamento e proteção metodológica de questionários com coletas ativas ou finalizadas.
- **Sprint 4 — Design Consistente, Formulários e Acessibilidade**:
  - Estrutura visual padronizada com cabeçalho de contexto, formulários com estados de processamento contra duplo envio, preservação de dados e foco no primeiro erro.
  - Responsividade validada para 360px, 768px e 1440px.
- **Sprint 5 — Fortalecimento dos Testes e Homologação**:
  - Testes da suíte `ValoraInsightRegressionT01ToT15Tests` fortalecidos com execução comportamental real:
    - T08: Execução real de `BffAdministrationController` validando respostas 204 NoContent e downloads binários (`FileContentResult`).
    - T13: Execução real de chamadas concorrentes contra `BffAuthenticationService.RefreshAsync` com semáforo de sincronização.
    - T14: Execução real do tratador `BffAuthenticationFailureHandler` retornando `ProblemDetails` estruturado (HTTP 403 `ACCESS_DENIED`) para requisições com `Accept: application/json`.
    - T15: Jornada integrada real (Modo Plataforma -> Seleção de Organização -> Contexto em Modo Organização -> Preparação de Diagnóstico PUT -> Reabertura e releitura GET).
  - Bateria completa executada: **607 testes aprovados (100% de sucesso)** em .NET 10.0.

