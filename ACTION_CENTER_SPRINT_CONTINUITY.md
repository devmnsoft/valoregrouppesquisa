# Continuidade — ActionCenter operacional

## Incremento — acompanhamento de evolução verificável (2026-09-16)

### Inventário curto

| Fluxo | Implementação encontrada | Lacuna | Ação deste incremento |
|---|---|---|---|
| Resultado → plano | Resultado administrativo vincula o plano à origem persistida; ActionCenter mantém ciclo de vida, revisão e histórico | Nenhuma regressão observada no caminho inspecionado | Preservado como fonte canônica, sem duplicar comandos no Evolution |
| Plano → execução → revisão | Central, detalhe, registros e revisão dedicada já existem com escopo organizacional, versão e idempotência | Fora do recorte alterado, integração PostgreSQL ainda depende do ambiente de testes | Preservado; métricas operacionais continuam distintas de maturidade |
| Ciclo → acompanhamento | Evolution já persistia ciclos e snapshots | Controller não exigia as permissões específicas; formulário perdia valores; snapshot aceitava ciclo encerrado e calculava variação com a mesma pontuação corrente, sem demonstrar comparabilidade | Políticas aplicadas, modelo tipado e limites adicionados, lock/estado revalidados e variação artificial removida |
| Comparação entre avaliações | Não havia regra explícita reutilizável | Versão, dimensões, escala, cálculo, população e disponibilidade não eram verificadas em conjunto | Serviço determinístico só calcula variação absoluta quando todas as bases coincidem; testes cobrem cada incompatibilidade |

### Decisões e evidências

- Snapshots operacionais preservam ações concluídas, atrasos e alertas, mas deixam `delta` nulo e a tendência como amostra insuficiente enquanto não houver duas avaliações com metadados comparáveis.
- A comparação devolve a limitação em vez de percentual artificial e declara que variação observada não prova causalidade.
- Criação de ciclos e registro de acompanhamento preservam os textos após erro, incluem ajuda contextual e devolvem o foco ao gatilho ao fechar o diálogo.
- O servidor exige `evolution.read`, `evolution.manage` e `evolution.snapshots.generate` nos respectivos caminhos, além do contexto de organização resolvido.

### Pendências e próximo incremento

- Ligar seletores pesquisáveis de diagnóstico, resultados e planos aos catálogos autorizados existentes e persistir os vínculos sem solicitar UUID manual.
- Materializar a assinatura metodológica a partir das versões publicadas e resultados persistidos; então apresentar avaliações lado a lado e usar o serviço de comparabilidade antes de exibir a variação.
- Executar build/Razor e testes .NET com o SDK 10.0.100, integração em PostgreSQL e percurso autenticado nos cinco viewports. Este contêiner não possui `dotnet`, `psql`, Docker nem credenciais de sessão; essas evidências não foram simuladas.

## Incremento — Forms/Builder confiável (2026-09-15)

- O fluxo prioritário Forms/Builder foi rastreado em `FORM_OPERATIONS_MATRIX.md`, separado explicitamente do frontend Firebase legado.
- A criação agora preserva o modal e os valores em falha, evita duplo envio, confirma a intenção e só navega ao construtor após receber o identificador persistido.
- O Builder deixou de disparar autosave concorrente a cada digitação, reidrata a seleção após consultas e expõe o arquivamento lógico de opções.
- Tipos de pergunta foram alinhados ao contrato (`description`) e o repository impede criar/editar opções em tipos incompatíveis, bem como trocar o tipo enquanto opções ativas existirem.
- O SDK .NET 10.0.100, PostgreSQL e uma sessão autenticada continuam indisponíveis; testes reais de persistência, Razor e viewports permanecem explicitamente pendentes na matriz.

## Incremento — encerramento resiliente e correção Razor (2026-09-15)

### Realizado

- Eliminada a colisão da variável local `page` com a diretiva Razor, mantendo o parâmetro HTTP `activityPage`, filtros, paginação e retorno local.
- Rascunhos de resultado/evidência são isolados por um escopo opaco derivado da organização e do usuário autenticados, além do plano; a confirmação nunca é restaurada.
- O rascunho só é apagado pelo descarte explícito ou pelo marcador de sucesso emitido após a conclusão no servidor. Falhas de validação, rede ou armazenamento preservam o uso normal do formulário.
- Conflitos mantêm textos e versão enviada, desmarcam a confirmação, distinguem a mensagem e oferecem consulta das alterações e início consciente de uma revisão atual.
- O fluxo sem JavaScript mantém POST MVC, antiforgery, validação por campo, confirmação tipada e revalidação transacional/idempotente já existente.

### Pendente e próximo passo

- O SDK .NET 10.0.100, PostgreSQL de teste e uma sessão autenticada continuam indisponíveis neste contêiner. Assim, o próximo passo é executar restore/build/testes (incluindo Razor), os cenários concorrentes no banco e a inspeção visual autenticada nos cinco viewports antes de integrar.
- Após as verificações obrigatórias do PR, o merge e a atualização `pull --ff-only` da base devem ser realizados por um operador com as credenciais e proteções do repositório disponíveis; nenhuma dessas etapas é antecipada aqui.

Revisão realizada a partir do `HEAD` `0dc7419` (PR #568), preservando o incremento anterior.

## Matriz de implementação

| Funcionalidade | Evidência no código | Lacuna encontrada | Alteração | Teste/evidência |
|---|---|---|---|---|
| Filtro por responsável | `ActionCenterController` e `ActionItemRepository` | O filtro de páginas de leitura chamava o catálogo de atribuição protegido por `Action.Manage` | Endpoint de leitura próprio e consulta limitada aos responsáveis presentes nos planos/atividades que o usuário pode ver, sempre isolada por organização | Teste JS de componentes; consulta parametrizada revisada |
| Atribuição | `ResponsibleOptions` e `Create` do repositório | Era compartilhada indevidamente com o filtro | Endpoint de atribuição continua exigindo `Action.Manage`; gravação continua revalidando usuário ativo da mesma organização dentro da transação | Contratos de execução existentes e isolamento na instrução `INSERT ... SELECT` |
| Seletor paginado | `responsible-selector.js` | Seleção inicial imutável, retry ambíguo e busca global no documento | Estado por componente, seleção atual preservada, abort/versionamento, retry da página exata, mensagens distintas, 401/403 e acessibilidade | `node --test tests/action-center/responsible-selector.test.js` |
| Navegação pós-comando | `ActionCenterController.ExecuteCommand` | Sucesso descartava retorno e erro voltava para uma lista fixa | Contexto tipado com retorno/histórico, allowlist local e propagação no sucesso/erro | Inspeção dos redirecionamentos e build pendente por SDK ausente |
| Criação de atividade | `CreateItem` e `PlanDetails.cshtml` | O retorno do plano era confundido com o retorno da atividade e podia apontar o cabeçalho para o próprio detalhe | Separados retorno da lista, URL corrente do plano e retorno da atividade; página interna preservada | Fluxo codificado nos campos ocultos e redirect |
| Duplo envio | `form-submit-state.js` | Estado podia permanecer ao restaurar via histórico e interferir com validação cancelada | Bloqueio posterior aos validadores, restauração em `pageshow` e reativação apenas dos controles bloqueados pelo módulo | Verificação de sintaxe JavaScript |

## Validação e limitações desta execução

- O SDK exigido é .NET `10.0.100`, conforme `backend/global.json`.
- O contêiner desta execução não possui o executável `dotnet`; portanto restore, build, testes .NET e compilação Razor permanecem **não validados**, e não foram simulados nem contornados.
- Não há cliente PostgreSQL nem instância de teste configurada no ambiente; a execução real das consultas permanece **não validada**.
- Os testes comportamentais do seletor e as verificações de sintaxe JavaScript foram executados localmente com Node.js.
- A validação visual autenticada nos cinco viewports permanece **não validada**, pois não há aplicação compilada/executável nem credenciais/fixture autenticada neste contêiner.

## Incremento — dashboard comercial e criação confiável (2026-09-14)

Linha de base confirmada no `HEAD` `4ca5fb7` (PR #569); seletores, navegação e proteção de envio existentes foram preservados.

| Problema observado | Alteração necessária | Teste de aceitação |
|---|---|---|
| Cards não navegavam ou aproximavam a consulta (críticas podiam incluir encerradas e ausência de responsável não tinha representação própria) | Filtros tipados `Open`, `Active` e `Assigned/Unassigned`, usados nos links, contagens e SQL sem UUID sentinela | Abrir cada indicador e comparar o total da listagem com o card |
| Dashboard calculava atraso por instante (`due_at < now()`), enquanto listas trabalhavam por dia | Comparar o dia civil no `time_zone` da organização e persistir prazo de formulário no fim do dia civil local | Ontem atrasa; hoje e amanhã não; nulo não atrasa; concluída/cancelada não entra no indicador |
| Criação direta validava somente responsável e não era idempotente | Validar ator, módulo, organização, origem/referências, datas e responsável dentro da transação; registrar chave+hash+resultado por organização, autor e operação | Repetição equivalente retorna o plano original; payload divergente gera conflito e preserva o formulário |
| Dashboard e formulário tinham baixa hierarquia operacional | Cabeçalho compacto, cinco KPIs acionáveis, consultas rápidas, agenda, acompanhamento e formulário em seções responsivas com revisão | Navegação por teclado e uso sem rolagem horizontal da página nos cinco viewports-alvo |

### Validação deste incremento

- JavaScript novo e scripts associados passaram em verificação de sintaxe; os testes comportamentais do seletor passaram.
- O SDK .NET `10.0.100` continua indisponível neste contêiner (`dotnet: command not found`), portanto restore, build, testes .NET e compilação Razor não puderam ser executados.
- Não há `psql`, Docker ou instância PostgreSQL configurada; a migração e os cenários de integração SQL não puderam ser executados contra banco real.
- Sem aplicação compilável, credenciais e navegador automatizado disponível, o percurso autenticado e as capturas dos cinco viewports permanecem pendentes; a responsividade foi tratada em CSS sem `zoom`, `scale` ou ocultação global.

## Incremento — gestão operacional controlada (2026-09-14)

- A navegação de retorno agora admite a Central de Ações em um único validador local, preservando consultas e páginas sem admitir destinos externos ou o próprio detalhe.
- Edição, atribuição e reagendamento de atividades usam versão esperada, chave de comando, bloqueio transacional, detecção de intenção divergente e histórico antes/depois.
- Responsável e prazo do plano possuem operações independentes; nenhuma delas propaga alterações silenciosas às atividades.
- A criação de plano aceita o administrador global somente pelo contexto confiável do servidor, preserva o usuário real como autor e revalida acesso e contratação antes de responder a replay.
- A consulta da agenda passou a aplicar explicitamente a mesma condição de módulo contratado usada pelos totais e listas.
- A migration aditiva acrescenta versão aos planos, valores legíveis ao histórico de atividades e histórico de alterações de planos.

### Evidência e pendências desta execução

- A validação JavaScript foi executada. O contêiner continua sem o SDK .NET 10.0.100, PostgreSQL e navegador autenticado; por isso build/Razor, integração transacional e capturas nos cinco viewports não foram declarados como validados.

## Incremento — revisão, histórico visível e intenção preservada (2026-09-14)

- O formulário dedicado de edição de plano agora usa um único prefixo tipado (`Command`) entre Tag Helpers, POST e ModelState; as quatro prioridades são vinculadas pelo valor real.
- Os diálogos de edição, atribuição e prazo preservam `CommandId`, versão e valores submetidos ao reabrir após validação ou conflito. A ausência de responsável é mantida como uma escolha explícita, sem fallback para o valor persistido.
- O detalhe do plano consulta `action_plan_change_history` com isolamento organizacional e de recurso, filtro de operação, paginação estável e autoria histórica. A interface traduz operações e diferenças de edição para linguagem de negócio sem apresentar JSON bruto.
- Capacidades de edição, atribuição e reagendamento das atividades passaram a ser propriedades explícitas do contrato; a interface não as infere mais das transições de progresso.
- A comparação de reagendamento lê o prazo como data civil no fuso da organização antes de decidir se existe mudança, evitando incrementos falsos de versão na virada UTC.

### Mapa do fluxo revisado

| Operação | Entrada | Autorização | Validação | Persistência/histórico | Retorno |
|---|---|---|---|---|---|
| Editar plano | Página dedicada tipada | `Action.Manage` + escopo do plano | DataAnnotations + serviço + versão | transação em `action_plans` e `action_plan_change_history` | URL local preservada |
| Atribuir/reagendar plano | Diálogo de revisão | `Action.Manage` + escopo do plano | intenção, elegibilidade/data civil e versão | atualização e histórico atômicos/idempotentes | detalhe, filtro e página histórica preservados em erro |
| Editar/atribuir/reagendar atividade | Diálogo acessível | capacidade explícita + endpoint `Action.Manage` + escopo | DataAnnotations + regras operacionais + versão | atualização e histórico atômicos/idempotentes | detalhe e página histórica preservados |
| Consultar histórico do plano | seção Histórico | `Action.Read` + organização + acesso ao plano | filtro allowlist e página limitada | consulta somente leitura na estrutura existente | paginação/filtro/retorno preservados |

### Validação e limitações

- A verificação de sintaxe do JavaScript e `git diff --check` foram executados.
- O SDK definido (`10.0.100`) não está instalado neste contêiner (`dotnet: command not found`); restore, build, Razor e testes .NET não puderam ser executados.
- Não há PostgreSQL de teste, navegador autenticado ou fixture de credenciais disponíveis; migrations/consultas reais e homologação visual nos cinco viewports permanecem pendentes e não são declaradas concluídas.

## Incremento — ciclo de vida canônico dos planos (2026-09-14)

### Matriz de transições

| Estado atual | Ação | Próximo estado | Permissão | Pré-condições | Efeito nas atividades |
|---|---|---|---|---|---|
| `draft` | Submeter | `proposed` | `action.manage` | conteúdo, origem/evidência, responsável e datas válidos | nenhum |
| `proposed` | Devolver | `draft` | `action.approve` | justificativa obrigatória | preserva todas |
| `proposed` | Aprovar | `approved` | `action.approve` | versão atual | vincula aprovação à nova versão; nenhum início automático |
| `approved` | Iniciar | `in_execution` | `action.manage` | aprovação atual e responsável elegível | habilita execução; não inicia atividades |
| `in_execution` | Concluir | `completed` | `action.complete` | resultado/evidência e nenhuma atividade aberta; ao menos uma concluída quando houver apenas canceladas | preserva atividades e evidências |
| qualquer não encerrado | Cancelar | `canceled` | `action.manage` | justificativa e nenhuma atividade aberta | não cancela em cascata |

Atividades podem ser preparadas antes da execução, mas comandos de execução são serializados pelo lock do plano. Registros legados fora da sequência são preservados e aparecem como pendência de regularização. Alterações materiais (título, descrição, prioridade ou resultado esperado) em plano aprovado invalidam a aprovação e devolvem o plano à avaliação; durante a execução são bloqueadas. Alterações operacionais de responsável e prazo preservam a aprovação.

### Validação e limitações deste incremento

- Os testes JavaScript e as verificações estáticas locais foram executados.
- O SDK .NET `10.0.100` continua ausente (`dotnet: command not found`), impedindo restore, build, Razor e testes .NET.
- Não há PostgreSQL de teste nem aplicação autenticada executável; a migração, os cenários concorrentes reais e a validação visual nos cinco viewports permanecem pendentes. Nenhuma captura foi produzida porque o aplicativo não pôde ser iniciado.

## Incremento — prontidão, avaliação e encerramento coerentes (2026-09-15)

- O braço desconhecido da expressão de transição agora usa diretamente `throw`, com a tupla explicitamente tipada como `(string From, string To)`, eliminando o CS8115 sem alterar a versão da linguagem.
- Submissão e início revalidam, dentro da transação, que o responsável existe, está ativo e pertence à organização. O início também exige uma aprovação registrada; mudanças operacionais não invalidam a aprovação, enquanto edição material registra um evento explícito de invalidação e retorna o plano à avaliação.
- A conclusão exige pelo menos uma atividade concluída inclusive quando o plano não possui atividades. Atividades abertas continuam impedindo tanto conclusão quanto cancelamento, sem cascata silenciosa.
- A consulta de prontidão passou a expor etapa, ações do estado, impedimentos, avisos, contagens e destinos tipados. As capacidades da tela são explícitas por ação e a lista oferece a consulta rápida **Aguardando avaliação** somente ao perfil aprovador.
- O detalhe apresenta avisos e impedimentos vindos da consulta do servidor, preserva a revisão de encerramento mesmo quando há bloqueios e traduz o evento de aprovação invalidada no histórico.

### Validação e limitações desta execução

- Os testes JavaScript do ActionCenter e as verificações de sintaxe passaram.
- O executável `dotnet` não está instalado e o download do SDK 10.0.100 foi bloqueado pelo proxy com HTTP 403; restore, build, Razor e testes .NET não puderam ser executados neste contêiner.
- `VALORA_TEST_POSTGRES_CONNECTION` não está configurada e não há `psql`, Docker, aplicação autenticada ou navegador disponível. Assim, integração PostgreSQL, concorrência real e capturas nos cinco viewports continuam pendentes, sem declaração de homologação.

## Incremento — revisão dedicada e resumo final (2026-09-15)

- Prontidão passou a orientar a etapa atual: preparação, avaliação, início ou encerramento, sem apresentar uma nova conclusão a planos encerrados. Cada impedimento corrigível possui destino próprio e a interface só oferece comandos compatíveis com a capacidade do usuário.
- A conclusão revalida sob o lock transacional o responsável elegível, a evidência de origem, o prazo e as contagens de atividades usadas pela consulta de prontidão.
- Participantes de atividades usam o mesmo escopo de leitura no detalhe, prontidão e histórico; impressão e revisão reutilizam esse escopo no servidor.
- O encerramento ganhou página dedicada, preservando versão, chave de intenção, resultado e evidência no fluxo canônico. Planos encerrados oferecem resumo HTML para impressão, sem inferência de evolução metodológica.

### Validação e limitações

- O ambiente continua sem o SDK .NET 10.0.100, PostgreSQL e navegador autenticado. Portanto build/Razor, concorrência contra banco e evidências nos cinco viewports não puderam ser executados; nenhuma homologação foi declarada.

## Incremento — resultado → relatório → plano (2026-09-15)

### Implementado

- A consulta administrativa do resultado passou a ser um contrato tipado e tenant-scoped que reúne diagnóstico, organização, período, versão imutável do formulário, estado de processamento, valores efetivamente calculados, dimensões, relatórios emitidos e planos vinculados. Ausência de cálculo não é convertida em zero.
- A transação de cálculo agora materializa também a identidade de `results` vinculada ao `result_score`, permitindo que relatórios e planos preservem a referência ao processamento específico.
- A tela executiva distingue espera, processamento pendente, processamento, insuficiência, disponibilidade e falha; ações dependentes só aparecem com um resultado persistido e explicam o impedimento quando indisponíveis.
- O relatório por resposta usa a mesma projeção persistida exibida na tela, preserva a versão e o resultado, reutiliza entregável válido da mesma origem/formato em repetição técnica e oferece download autenticado com tenant e permissão revalidados.
- A criação no ActionCenter aceita origem `result`, preserva `result_id`/`origin_id`, oferece somente responsáveis autorizados pelo seletor existente, salva como rascunho e mantém o `CommandId` existente para recuperação idempotente. O retorno ao resultado é limitado a URL local permitida.
- Planos vinculados voltam à página do resultado com responsável, prazo, estado e progresso calculado pela regra já usada nas atividades; a evidência de origem permanece separada da evidência de conclusão.

### Verificação e limites reais

- Verificados localmente: sintaxe dos scripts da página de resultado e relatórios, integridade do diff e contratos/SQL por inspeção.
- Bloqueados neste contêiner: SDK .NET 10, `psql`, Docker, PostgreSQL de teste e sessão autenticada. Portanto restore, compilação Razor, testes .NET, persistência/reconsulta em conexão nova, autorização HTTP, download real e inspeção visual nos cinco viewports permanecem **não verificados**.
- A geração disponível neste módulo permanece síncrona e nos formatos JSON executivo/CSV já suportados; não foi criada fila paralela nem foi declarado PDF inexistente. Reprocessamento analítico continua fora deste incremento: vínculos históricos são preservados, mas a política de solicitação/reexecução ainda requer o fluxo canônico de processamento.
