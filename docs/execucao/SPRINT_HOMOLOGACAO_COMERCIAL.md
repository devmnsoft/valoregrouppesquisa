# Continuidade — Sprint de homologação comercial

Atualizado em 2026-09-10. Este é o documento único de continuidade desta sprint. Os estados abaixo distinguem **implementado**, **testado** e **não verificado**; build isolado não equivale à homologação do fluxo.

## Baseline

- Branch local: `work`; HEAD inicial `77f0520` (merge do PR #553), exatamente a referência auditada solicitada.
- O checkout não possui remoto configurado. O comando `git fetch --all --prune` foi executado, mas não havia remoto a consultar; portanto não foi possível comparar commits posteriores, fazer `push` ou integrar a branch-base.
- Árvore inicial sem alterações locais. Nenhum `AGENTS.md` foi encontrado no repositório, em seu diretório pai ou nos caminhos globais inspecionados.
- Referências metodológicas consultadas: `docs/VALORA_METHODOLOGY_ALIGNMENT.md` e os contratos/testes existentes. O próprio documento registra que os PDFs metodológicos citados anteriormente não estão no workspace; conteúdo ausente não foi presumido.
- Banco de produção não foi acessado. `VALORA_TEST_POSTGRES_CONNECTION` não estava configurada e o SDK `dotnet` não está instalado neste container.

## Bloco A — parâmetro `wide`

### Achado confirmado

**Implementado:** `WorkspaceRepository.MyDayAsync` usava `@wide` no SQL, mas o helper criava um objeto contendo apenas `o` e `u`. O PostgreSQL recebia um placeholder sem parâmetro Dapper correspondente e o interpretava como identificador, produzindo `42703: column "wide" does not exist`.

### Decisão e correção

**Implementado:** o helper passou a receber explicitamente `object parameters`. Todas as consultas sujeitas à regra de visibilidade enviam `{ o, u, wide }`. Não foi criada coluna, removido filtro, concatenado booleano nem convertido erro em lista vazia. O predicado de organização permanece obrigatório. Itens sem proprietário continuam compartilhados porque essa é a regra já expressa pelo SQL vigente.

**Implementado:** a API não lê mais `organization_id` diretamente no controller do Workspace. Ela usa `ICurrentRequestContext`, inclusive a organização selecionada pelo mecanismo central para o administrador global. A visão ampla é derivada do contexto autenticado (`IsGlobalAdministrator` ou papel `admin_cliente`); não existe parâmetro HTTP `wide` controlado pelo navegador. Requisições sem usuário válido são recusadas.

**Implementado:** a mesma expressão de visibilidade (item próprio, compartilhado ou visão ampla autorizada) passou a proteger Meu Dia, fixação, fixados e recentes. Fixar um UUID privado de terceiro não cria registro e retorna acesso negado; fixados e recentes são reavaliados a cada leitura, de modo que uma troca posterior de proprietário revoga a exposição. As ordenações tocadas ganharam desempate por UUID.

### Evidência de teste

- **Implementado, não executado localmente:** teste PostgreSQL exercita visão restrita e ampla, itens próprios/de terceiros/sem proprietário, isolamento entre duas organizações, recusa de pin privado, revalidação de fixados/recentes depois da troca de proprietário e pin/unpin repetidos. O teste recusa nomes de banco sem marcador `test`, `teste`, `homolog` ou `qa` e remove apenas os UUIDs aleatórios criados por ele.
- **Implementado:** sem `VALORA_TEST_POSTGRES_CONNECTION`, o teste agora produz skip explícito em vez de retornar como aprovado. O workflow PostgreSQL define e verifica a variável e executa obrigatoriamente a categoria `DatabaseContract` depois de aplicar o schema canônico.
- **Implementado, não executado localmente:** contrato estático protege o binding de `wide`, a assinatura do helper e a derivação server-side no controller.
- **Não verificado:** endpoint completo autenticado contra PostgreSQL, perfil não autorizado tentando elevar escopo e execução do teste PostgreSQL. Esses cenários permanecem gate obrigatório de CI/homologação; a ausência de infraestrutura não é aprovação.

## Revisão dirigida das regressões conhecidas

- **Não verificado nesta entrega:** materialização de `FormRow`, `FormListItemResponse` e `SubscriptionRow`; tipos opcionais de `CompaniesAsync`; duplicidade em `ProcessRepository`; DI de `BenchmarkRepository`; strings de `FormalDeliverableRepositories`; Razor/CSS de Benchmarks e Indicators. Devem ser confirmados por consulta executada e testes específicos, não apenas busca textual.
- **Não verificado nesta entrega:** BFF público, sessão autenticada, downloads binários, concessões por aba, exportações, leases de workers e transições de campanhas.
- **Não verificado nesta entrega:** padronização visual, formulários, responsividade e Playwright nas cinco viewports. Nenhuma tela foi alterada nesta entrega e, por isso, não se declara evolução visual concluída.

## Blocos B–F e seed de homologação

- **Não implementado:** seed sintético canônico. Antes de incluí-lo em `backend/database/postgresql/script_completo.sql`, ainda é necessário mapear integralmente constraints, mecanismo seguro de provisionamento de identidade, capacidades contratadas e efeitos externos. Não foi criado SQL concorrente nem pseudoseed inseguro.
- **Não testado:** primeira e segunda execução idempotente, referências, isolamento, processamento metodológico, publicação/campanha/resposta/resultado, entregáveis e exportação.
- **Não verificado:** PostgreSQL e Redis locais, hosts `Valora.Api`/`Valora.Web` e Playwright integrado.

## Como executar a regressão PostgreSQL com segurança

1. Provisione banco descartável já migrado pelo script canônico, cujo nome contenha `test`, `teste`, `homolog` ou `qa`; nunca use produção.
2. Defina `VALORA_TEST_POSTGRES_CONNECTION` somente no processo de teste.
3. Execute `dotnet test backend/Valora.Tests/Valora.Tests.csproj -c Release --filter WorkspaceRepositoryPostgresTests`.
4. Confirme no relatório que o teste `Workspace_queries_enforce_visibility_tenant_and_idempotent_pins` foi realmente executado. Sem a variável, ele não toca banco e a homologação PostgreSQL continua pendente.

## Bloqueios, riscos e próximo passo

1. **P0:** disponibilizar .NET SDK 10 e PostgreSQL descartável migrado; executar a regressão e acrescentar teste de host autenticado para contexto ausente, troca de cliente e tentativa de ampliar escopo.
2. **P0:** configurar remoto/autorização para comparar a branch-base, fazer `push` e abrir PR remoto. Não há evidência local de commits posteriores a `77f0520`.
3. **P1:** desenhar e revisar o seed transacional opt-in no SQL canônico, incluindo trava inequívoca de ambiente e desativação de integrações externas, antes de inserir fixtures.
4. **P1:** executar os blocos C e D (sessão/BFF/downloads; campanhas/exportações/workers) com testes de host e PostgreSQL.
5. **P2:** somente então concluir Workspace/formulários/responsividade e executar Playwright real com API, Web, PostgreSQL e Redis.

Próximo passo concreto: instalar o SDK exigido, configurar um PostgreSQL descartável, aplicar `script_completo.sql` e executar o filtro `WorkspaceRepositoryPostgresTests`; falha nesse gate bloqueia o avanço da homologação.

## Incremento operacional — prioridades (2026-09-10)

### Matriz de estados implementada

| Estado atual | Comando | Requisitos | Estado final | Histórico |
|---|---|---|---|---|
| `active` | editar/atribuir | `priorities.manage`, responsável ativo do mesmo cliente, origem visível, versão atual | `active` | `edited`, uma vez por edição |
| `active` | progresso | `priorities.manage` e escopo autorizado, 0–99, observação e versão atuais, `commandId` inédito | `active` | `progress`, idempotente por comando |
| `active` | concluir | `priorities.manage`, escopo autorizado, justificativa e versão atuais | `completed`, 100% | `complete`, idempotente por comando |
| `active` | cancelar | `priorities.manage`, escopo autorizado, justificativa e versão atuais | `cancelled`, preserva progresso | `cancel`, idempotente por comando |
| `completed`/`cancelled` | reabrir | `priorities.manage`, escopo autorizado, justificativa e versão atuais | `active`, preserva progresso | `reopen`, idempotente por comando |

Qualquer outra combinação é rejeitada. Itens encerrados não aceitam edição ou progresso. `updated_at` é o token de concorrência, e atualização da prioridade, espelho no Workspace e histórico usam a mesma transação. A origem opcional somente é aceita quando corresponde a um item visível do mesmo cliente; o responsável precisa estar ativo no mesmo cliente.

### Entregue e limites da verificação

- **Implementado:** detalhes, edição, responsáveis pesquisáveis pelo seletor nativo, progresso, histórico, conclusão, cancelamento e reabertura; permissões `priorities.read/manage` nos endpoints; registro explícito de abertura em recentes; pin/unpin; feedback e confirmação; proteção de duplo envio; conflito; estados vazios por seção; “Ver todos”; layout responsivo.
- **Implementado:** evolução idempotente do SQL para tipo de evento e chave de comando, índices de histórico/idempotência e constraints de estado/conclusão.
- **Implementado:** banco do workflow renomeado para `valorapesquisa_test_ci`, TRX publicado e gate de descoberta diferente de zero.
- **Não homologado neste container:** o SDK .NET 10, PostgreSQL e hosts oficiais não estão disponíveis. Assim, build, materialização Dapper e Playwright integrado permanecem gates obrigatórios de CI; não são declarados como aprovados.
- **Próxima sequência concreta:** executar restore/build/test com SDK 10; aplicar o SQL duas vezes em PostgreSQL descartável; executar `DatabaseContract`; iniciar API/Web oficiais e executar o fluxo Playwright nas cinco viewports. Corrigir qualquer divergência de materialização antes de merge.

## Consolidação do Workspace após a PR #555 (2026-09-10)

### Baseline e achados confirmados

- **Baseline confirmado:** esta etapa começou em `2a5bf03`, merge da PR #555, com árvore limpa e sem commits posteriores no checkout local. Não há remoto configurado; portanto `fetch`, `push`, abertura remota e integração posterior continuam indisponíveis.
- **Confirmado no código:** a listagem específica entregava `ExecutivePriorityDto` sem discriminador, enquanto a UI inferia o tipo pelo título traduzido da seção; havia ainda `div` diretamente sob `ul`, comandos baseados em `prompt`/`confirm`, reabertura de concluída forçando 99%, opções truncadas em 100 e resposta completa sem paginação.
- **Confirmado no código:** `allowedActions` concedia comandos ao proprietário sem permissão de gerenciamento, embora os endpoints exigissem `priorities.manage`; o agregado não declarava `priorities.read`; o registro de recente acontecia antes da leitura no navegador; e a idempotência considerava somente a existência de `command_id`.
- **Confirmado no SQL:** o backfill ignorava qualquer colisão com `ON CONFLICT DO NOTHING` e os `CHECK` eram criados sem diagnóstico prévio de linhas legadas incompatíveis.

### Correções e funcionalidades implementadas

- O contrato de prioridade agora carrega `itemType=priority`; renderização usa discriminadores de contrato (`itemType`, `resultType` ou tipo conhecido do endpoint), não títulos. Listas usam `li` diretamente, e busca distingue ação, recurso e prioridade.
- A listagem completa de prioridades ganhou página/tamanho validados, total autorizado, `hasMore`, ordenação determinística e filtros server-side por situação, nível, responsável e prazo. O resumo permanece separado e limitado a oito prioridades ativas. Seletores de responsável e origem ganharam busca paginada e inclusão autorizada da referência atual.
- Os quatro comandos usam diálogo acessível, campos tipados, consequência contextual, validação, resumo/erro por campo, foco, loading, bloqueio de envio repetido e retorno de foco. Cancelar não cria nem envia comando. A chave permanece na mesma intenção para retry. Reabrir preserva o percentual (inclusive 100), separando estado e medição.
- `allowedActions` passou a refletir estritamente `priorities.manage`; proprietário sem gerenciamento conserva leitura no escopo, mas não recebe comandos. O agregado declara a mesma política de leitura da listagem específica, os endpoints de opções continuam protegidos por gerenciamento e o botão de criação é renderizado somente após autorização pela política.
- O detalhe é carregado antes de registrar o acesso recente. Falha não crítica do registro é escrita no log e não impede a resposta válida; cancelamento da requisição continua sendo propagado.
- Idempotência de transições agora compara organização, prioridade, ator, operação e hash do conteúdo/versão. Repetição idêntica retorna o estado corrente sem novo efeito; reutilização divergente retorna conflito. A linha é bloqueada para serializar concorrentes e a projeção do Workspace é conferida antes do commit.
- SQL canônico adiciona metadados de idempotência de forma repetível, diagnostica legado incompatível antes dos `CHECK`, falha explicitamente em colisão de ID e converge o espelho existente em vez de ignorá-lo. Conclusão/reabertura também convergem `completed_at`.
- Validações essenciais foram duplicadas na camada Application para que regras de título, nível, origem, progresso, justificativa, versão e chave não dependam somente de DataAnnotations do controller. Consultas tocadas usam `CommandDefinition` com `CancellationToken`.

### Testes e evidências

- **Executado com sucesso:** `node --check backend/Valora.Web/wwwroot/js/executive-workspace.js`, `node tools/validate-scriptbd-completo-sql-compat.js`, `npm run repository:boundaries`, `npm run web:premium-layout` e `git diff --check`.
- **Implementado, execução bloqueada pelo ambiente:** Playwright comportamental cobre listagem específica, Meu Dia, visão geral/fixados/recentes/ações e cancelamento sem requisição. O pacote está instalado, mas o Chromium não existe no cache; a tentativa de instalação foi recusada pelo CDN com HTTP 403. Por isso não há screenshot nem alegação de validação nas cinco viewports.
- **Não executado por limitação do ambiente:** restore/build/test .NET (SDK `dotnet` ausente), materialização Dapper/Npgsql e regressões PostgreSQL (conexão descartável não configurada), hosts oficiais Web/API e repetição real do SQL.

### Pendências e próximo passo

1. Em CI com .NET 10 e PostgreSQL descartável, executar restore, build Release, testes completos e `DatabaseContract`; aplicar o SQL duas vezes sobre banco limpo e cópia legada, incluindo fixtures incompatíveis esperadas.
2. Executar os testes Playwright contra Valora.Web/Valora.Api oficiais e capturar as cinco viewports. Confirmar via tráfego que troca rápida de filtro cancela resposta antiga e que retry reutiliza a chave.
3. Completar, com infraestrutura real, cenários de duas organizações, matriz de papéis/capability, rollback, colisão/backfill, concorrência e materialização de `DateTimeOffset`. A implementação está pronta para esses gates, mas eles não são declarados homologados.
4. Configurar remoto autorizado para fetch/rebase conforme política, push e PR. Não forçar merge nem inventar resultado de check externo.

## Sprint — consolidação funcional, navegação e suporte persistido (2026-09-10)

### Inventário e decisão

| Classificação | Funcionalidade | Objetivo / persona | Rota | Controller / serviço / repositório | Entidade | Comandos | Permissão / capability | Consumidores | Decisão e compatibilidade |
|---|---|---|---|---|---|---|---|---|---|
| A | Prioridades executivas | Compromissos acompanháveis; gestores e analistas | `/Workspace/Priorities` e antiga `/Priorities` | `WorkspaceController`; `ExecutivePriority*` | `executive_priorities` e histórico | criar, editar, progresso, concluir, cancelar, reabrir | `priorities.read/manage`; dashboard | menu, Workspace, busca e recentes | Um único item principal, em Execução, aponta ao Workspace persistido. A URL metodológica antiga permanece disponível, mas deixa de concorrer no menu. |
| A | Organização | Administração do cliente / administradores | `/Organization` | `OrganizationController` | `organizations` e configurações | consultar e configurar | papéis administrativos; `organization` | atalhos e URLs existentes | Mantido somente em Administração do cliente; URLs e autorização backend não foram removidas. |
| A | Usuários e perfis | Identidade do cliente / administradores | `/Users`, `/Users/Roles` | `UsersController`, serviços/repositórios de acesso | `users`, `roles`, vínculos e permissões | gestão conforme permissão | `users`, `identity` | administração e links compartilhados | Removidas entradas concorrentes; destinos canônicos continuam inalterados. |
| A/C | Dicionário e mapa cognitivo | Governança metodológica e consulta de conceitos | `/Methodology/Dictionary`, `/Methodology/CognitiveMap` | `MethodologyController` / repositório metodológico | catálogo metodológico versionado | consulta; manutenção só autorizada | `organizational_intelligence`; papéis distintos | Metodologia e Inteligência | Um item por destino. Não houve fusão de dados nem de permissões; consumidores por URL continuam válidos. |
| A/C | Evolution e Journey | Mudança entre ciclos versus memória de eventos | `/Evolution`, `/Journey` | controllers e repositórios próprios | ciclos/snapshots versus `journey_events` | ciclos e consulta histórica | inteligência organizacional | relatórios, Action Center e links | Duplicatas de cada destino foram removidas, mas Evolution e Journey foram preservados como capacidades distintas, com nomes em português. |
| A | Qualidade de dados | Operação técnica não destrutiva | `/AssistedOperations/DataQuality` | `AssistedOperationsController` / repositório operacional | issues e logs | consulta/operação autorizada | administração | Dados e Plataforma | Um item principal em Dados; URLs antigas continuam resolvidas. |
| A | Integrações | Entregas e conexões do cliente | `/Intelligence/Integrations` | `IntelligenceController` / integração | integrações, chaves e eventos | conforme permissão | integração/inteligência | Dados, administração | Um item de dados por destino. A console global de Administração é preservada por ter finalidade e contrato próprios. |
| D | Feature flags | Configuração técnica / admin Valora | `/Administration/FeatureFlags` | `AdministrationController` / BFF admin | configuração técnica | configurar conforme autorização | `settings.read` e admin Valora | console global | Preservada e descrita explicitamente como técnica; não concede contrato nem acesso. |
| D | Módulos contratados | Direitos comerciais vigentes / admin do cliente | `/Saas/Subscription` | `SaasController`, contratos SaaS | assinatura, módulos e limites | somente consulta nesta sprint | contrato e organização | área SaaS | Destino canônico é Minha Assinatura. Removido o falso apontamento para Feature Flags; não houve preço, cobrança ou ativação. |
| B/E | Suporte do cliente | Abrir e acompanhar solicitações / usuário autenticado | `/SuccessCenter/Support` | `SuccessCenterController`, `SupportTicketService`, `SuccessCenterRepository` | tickets, messages, events | criar, responder, resolver e reabrir | organização autenticada; isolamento no SQL | `/Support`, `/Support/Tickets`, Success Center | Fluxo ilustrativo substituído por persistência real. GETs legados redirecionam; a fila `/Platform/Support` permanece distinta. |
| C | Fila global de suporte | Operação entre clientes / equipe autorizada | `/Platform/Support` | `AssistedOperationsController` | mesmo domínio de tickets, contexto selecionado | atendimento global (não ampliado nesta entrega) | perfil de plataforma | operação assistida | Preservada como visão distinta. Comentários internos permanecem filtrados da visão do cliente. |
| D | Planos e ações | Execução de compromissos / gestores | `/ActionCenter` | serviços e repositórios de plano/ação | `action_plans`, `action_items`, check-ins | criar plano/ação e acompanhar | inteligência organizacional | insights, alertas e decisões | Preservado sem fundir com prioridade, decisão, Journey ou Evolution. A integração específica prioridade→ação ainda requer incremento posterior e não é declarada concluída. |

### Implementação e garantias verificáveis

- O suporte agora só anuncia sucesso depois de `support_tickets` e o evento inicial serem confirmados na mesma transação. Listagem e detalhe sempre incluem `organization_id`; UUID de outro cliente resulta em `404` sem revelar existência.
- Respostas são recusadas para chamado encerrado. Resolver e reabrir bloqueiam a linha, validam transições no servidor e gravam evento na mesma transação. A visão do cliente exclui mensagens/eventos com metadado `internal=true`; nenhuma nota interna foi criada por este fluxo.
- O formulário mantém o modelo após erro, usa validação por campo/resumo e antiforgery. A tela de detalhes apresenta conversa cronológica e próxima ação; tabelas usam rolagem local e a composição quebra em uma coluna pelo grid existente.
- Rotas GET antigas de suporte encaminham para o fluxo canônico sem perda de corpo. Nenhum POST legado foi redirecionado, nenhum dado/tabela/permissão foi excluído e nenhuma fusão destrutiva foi executada.
- O catálogo não publica duas entradas para o mesmo par controller/action. Atalhos por URL continuam aceitos, e a autorização backend permanece independente da visibilidade do menu.

### Execução e limitações desta etapa

- **Executado:** inventário de endpoints, consumidores, contratos, tabelas e serviços; leitura do alinhamento metodológico, da central de atendimento e do plano de ação; `git fetch --all --prune` (checkout sem remoto configurado).
- **Executado:** validações estáticas Node/SQL e `git diff --check`, registradas no commit desta etapa.
- **Não executado por ambiente:** SDK .NET 10 ausente, PostgreSQL descartável/Redis não configurados e hosts Web/API indisponíveis. Portanto build, testes xUnit, aplicação dupla do SQL, consultas PostgreSQL, Playwright e capturas nas cinco viewports seguem pendentes; não são tratados como aprovados.
- **Não concluído:** prioridade → selecionar/criar plano → criar ação idempotente. Os domínios foram preservados e inventariados, mas a implementação existente não possui chave de comando nem vínculo dedicado suficiente para afirmar o critério. Nenhum ranking, sincronização silenciosa ou deduplicação por título foi inventado para mascarar essa lacuna.

## Sprint — execução integrada e suporte consistente (2026-09-10)

### Blocos A–C — prioridade para execução

- **Baseline confirmada:** etapa iniciada em `0cb284e`, merge da PR #557, branch local `work`, árvore limpa e sem remoto configurado. Não havia commits posteriores disponíveis no checkout. SDK .NET, cliente PostgreSQL e cliente Redis não estão instalados no container.
- **Implementado:** relação muitos-para-muitos `priority_action_links`, com organização, FKs, autor, data, exclusão lógica e unicidade apenas do vínculo ativo. Uma atividade pode atender mais de uma prioridade; ela não é recriada ao ser vinculada.
- **Implementado:** comando transacional cria opcionalmente plano, cria atividade e vincula prioridade na mesma unidade de trabalho. A chave é serializada por advisory lock e persistida com hash da intenção: retry idêntico devolve o mesmo resultado e conteúdo divergente gera conflito. Plano, prioridade e atividade são validados no mesmo cliente e recursos cancelados são recusados.
- **Implementado:** detalhes da prioridade distinguem seu progresso do progresso das atividades e exibem plano, situação, resultado esperado e evidência. O assistente tipado permite escolher atividade/plano por nome ou criar plano e atividade, sugere somente título/contexto editáveis, exige revisão e mantém a chave durante retry. Links abrem a atividade no Action Center; concluir a atividade não altera a prioridade.

### Bloco D — suporte

- **Implementado:** listagem paginada no PostgreSQL, ordenação estável, total coerente e filtros de situação, categoria, prioridade e período. Usuário comum enxerga somente chamados abertos por ele; administrador do cliente/equipe com `support_tickets.manage` enxerga a organização selecionada.
- **Implementado:** detalhe, resposta e transições verificam o mesmo escopo. `includeInternal` é derivado exclusivamente da autorização no servidor; usuário comum nunca ativa notas internas. A resposta digitada é preservada em falha recuperável e mensagens técnicas deixam de ser apresentadas nas transições.
- **Preservado:** criação transacional de chamado/evento inicial, isolamento por organização, rotas GET compatíveis e separação da fila global `/Platform/Support`.

### Evidências, limites e próximo passo

- **Executado:** validação sintática do JavaScript, validador de compatibilidade do SQL canônico e `git diff --check`.
- **Executado com ressalva preexistente:** testes unitários do validador de fronteiras passaram; a verificação final acusa dois SQL oficiais porque `valora-cenarios-workspace-77f0520.sql`, já presente na baseline, também está na pasta canônica.
- **Não executado por ambiente:** build/test .NET, PostgreSQL real (banco limpo, atualização e segunda aplicação), Redis, hosts Web/API e Playwright/capturas nas cinco viewports. Esses itens permanecem gates de CI/homologação e não são declarados aprovados.
- **Próximo passo:** em CI com .NET 10 e PostgreSQL descartável, executar build Release, testes completos/DatabaseContract, aplicar o SQL duas vezes e exercitar concorrência e matriz de papéis; depois iniciar hosts oficiais e capturar as cinco viewports.
