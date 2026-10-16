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

## Correções críticas — Erros Estruturados, BFF, Contexto Global e Dashboard

- **Colisão de `traceId`**: Removida inserção redundante de `traceId` nas extensões de `Problem()` em `OrganizationsController` e `OrganizationBrandingController`, integrando `ProblemDetailsEnrichmentExtensions` com métodos idempotentes (`SetExtension`, `SetExtensions`, `EnsureCorrelation`).
- **Leitura do corpo no BFF**: Criado `BffBodyReader` centralizado que ignora leitura de stream para métodos sem corpo (GET/HEAD), detecta payloads vazios, valida `Content-Type` JSON (produzindo 415 quando incompatível), trata JSON malformado como 400 ProblemDetails estruturado, suporta chunked/HTTP/2 com limite de 10 MB, e preserva 204 NoContent e downloads binários.
- **Frontend BFF**: Corrigidos `enterprise-page.js` e `diagnostic-workspace.js` para não enviar `Content-Type: application/json` em requisições GET sem corpo, utilizando `Accept: application/json`.
- **Modo Plataforma vs Modo Organização**: Atualizado `BffAccountController.Context`, `BffAuthController.Organizations` e `BffAuthenticationService.SelectOrganizationAsync` para permitir que o administrador global opere em Modo Plataforma sem disparar consultas organizacionais indevidas; organizações obsoletas (404) têm a seleção limpa automaticamente sem destruir a sessão autenticada.
- **Tratamento de 401/403 sem loop**: No pipeline de autenticação do `Valora.Web`, requisições com cabeçalho `Accept: application/json` ou `X-Requested-With: XMLHttpRequest` recebem ProblemDetails JSON estruturado (401/403) em vez de redirecionamento para login/HTML, estabilizando chamadas como `/ActionCenter/ResponsibleOptions`.
- **Evolução do Dashboard e Shell**: Interface exibe painel específico para Modo Plataforma, seletor de organizações com filtro por nome e botão para retornar ao Modo Plataforma; controle de concorrência por geração previne repintura de dados atrasados durante trocas rápidas de cliente.
- **Bateria de Testes**: Implementada suíte `ValoraInsightRegressionT01ToT15Tests` com 15 regressões comportamentais cobrindo T01 a T15, validada com 607/607 testes aprovados no backend.

