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
