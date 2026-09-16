# Dashboard e Workspace de entregáveis

Dashboard e workspaces reutilizam os endpoints de Inteligência Organizacional. O fluxo de criação por Insight agora envia `insightId` e `sourceType=insight`, permitindo que a API valide e preserve a evidência rastreável. O gateway `/bff/intelligence/{resource}` encaminha as rotas autenticadas sem substituir a autorização da API.

## Consolidação da entrada de trabalho — 2026-09-15

Dashboard e Workspace mantêm responsabilidades distintas: `/Workspace` é a entrada para o trabalho diário; `/Dashboard` continua sendo a leitura analítica especializada. A navegação contextual liga as duas superfícies, sem remover rotas ou históricos.

| Tela | Finalidade | Ação principal | Fonte | Permissão/módulo | Situação |
|---|---|---|---|---|---|
| Workspace | Identificar pendências e o próximo passo permitido | Abrir pendência ou ação rápida | `workspace_items`, prioridades, recentes/fixados e catálogo de atalhos | contexto organizacional e acesso ao registro; cada atalho exige módulo + permissão no servidor | Integrado; listas continuam limitadas pelo repository |
| Meu Dia | Trabalho vencido, crítico ou dependente de decisão | Abrir registro de origem | `workspace_items` tenant-scoped | proprietário/compartilhado; equipe apenas para perfil amplo derivado da sessão | Integrado; não trata concluído/cancelado como pendência |
| Prioridades | Gestão detalhada e paginada | Criar/acompanhar prioridade | `executive_priorities` e histórico | `priorities.read/manage` | Integrado e paginado |
| Dashboard | Análise executiva | Explorar indicadores e evolução | endpoints de inteligência | módulo/permissões da análise | Especializada; não duplicada no Workspace |
| Forms/Surveys | Preparar e executar diagnóstico | Criar formulário/diagnóstico | repositórios canônicos de Forms/Surveys | `forms.create`/`surveys.create` e módulo contratado | Atalhos somente quando autorizados |
| Results/ActionCenter | Consultar resultado e acompanhar compromissos | Consultar resultado/criar ou acompanhar plano | resultados e ActionCenter canônicos | `results.read`/`action.manage` e módulo contratado | Atalhos somente quando autorizados |

### Correções confirmadas

- A ação rápida, antes renderizada sem handler, agora registra a execução no endpoint autenticado e só navega para uma rota local retornada pelo servidor.
- O endpoint agregado e o endpoint específico usam a mesma filtragem de atalhos. Linhas desconhecidas no catálogo são negadas por padrão; ocultar no navegador não constitui autorização.
- Foram incluídos os percursos ausentes para criar formulário e consultar resultados. URLs inexistentes de criação direta foram substituídas pelas listagens canônicas, que aplicam sua autorização novamente.

### Continuidade

Próximo lote: substituir as projeções ainda dependentes de `workspace_items` por uma consulta agregada canônica de diagnósticos e atividades, com filtros de período/módulo/equipe e paginação independente por bloco; executar a fixture multi-organização em PostgreSQL; compilar Razor; e percorrer com sessão autenticada os cinco viewports. Estas verificações não são declaradas concluídas neste incremento.
