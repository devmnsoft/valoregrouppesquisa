# Continuidade — prioridade e execução

## Matriz verificada

| Fluxo | Implementação encontrada | Lacuna confirmada | Alteração realizada | Evidência de aceite |
|---|---|---|---|---|
| Prioridade → vínculo | API/BFF, assistente e tabelas de vínculo já existiam | consulta usava coluna inexistente e materializava UUID como `int` | leitura tipada, isolamento por organização e bloqueio `FOR UPDATE` preservado | testes de contrato e teste PostgreSQL com schema canônico |
| Reenvio idempotente | comando era único apenas por organização/chave | prioridade, operação e autor não eram comparados | fingerprint normalizado e contexto persistido/comparado sob advisory lock | repetir retorna o ID; trocar contexto resulta em conflito |
| Criar plano e atividade | inserções ocorriam na mesma transação | responsável e acesso ao plano não eram verificados; faltava evento Journey | validações transacionais de recurso/responsável e evento de criação | rollback integral em falha e histórico criado no sucesso |
| Assistente de execução | havia vínculo ou criação com checkbox | opções ambíguas, listas completas e campos inativos enviados | três percursos explícitos, busca paginada autorizada, resumo e campos desabilitados | navegação por A/B/C e ação final abre a atividade |
| Acompanhamento → conclusão | ActionCenter persiste check-ins, bloqueio e conclusão com evidência | detalhe ainda reúne formulários concorrentes e não expõe histórico completo | preservado nesta entrega para não duplicar regras sem contrato de histórico | próximo incremento abaixo |

## Concluído

- Correção do contrato SQL real de `executive_priorities`, sem criar `deleted_at` artificial.
- Idempotência contextual, autorização de prioridade/plano/atividade/responsável e atomicidade do vínculo.
- Migração canônica compatível com comandos legados e índice de consulta contextual.
- Assistente com três percursos, busca limitada a recursos autorizados, paginação, prevenção de duplo envio e abertura da atividade.
- Testes estáticos de regressão para os bloqueios identificados.

## Pendente / limitações reais

- Executar os testes PostgreSQL requer `VALORA_TEST_POSTGRES_CONNECTION` apontando para banco de teste/homologação.
- As opções de planos e atividades agora são filtradas, autorizadas, contadas, ordenadas e paginadas no PostgreSQL, com o mesmo predicado na contagem e na listagem.
- `ItemDetails` e `PlanDetails` ainda precisam de modelos compostos tipados para histórico, nomes de responsáveis e conflito otimista.
- Formulários alcançáveis ainda não inspecionados nominalmente: Administração SaaS, Pessoas, Diagnósticos, Pesquisas, Inteligência, Jornada e Evolução. Não há declaração de padronização global.
- Homologação visual autenticada nos cinco viewports depende de ambiente com identidade, API e PostgreSQL configurados.

## Próximo incremento

1. Extrair criação transacional de plano/atividade para um serviço ActionCenter que aceite `IUnitOfWork` sem abrir transação aninhada.
2. Criar modelos compostos de detalhe para plano e atividade, com histórico operacional paginado e versão otimista.
3. Separar check-in, bloqueio e conclusão em diálogos acessíveis compartilhados.
4. Executar fixture sintética de duas organizações e captura visual autenticada nos cinco viewports.

## Incremento — central de execução

### Concluído

- Endpoints de opções deixaram de materializar listas completas no controller. O repository aplica organização, acesso ao recurso, estados aptos, busca, ordenação estável, limite de página e contagem equivalente.
- O assistente carrega páginas adicionais, informa quantidade e disponibilidade de mais resultados, preserva a seleção, usa debounce, cancela requisições antigas e permite recuperação de erro.
- A criação seleciona responsável ativo pelo nome no contexto da organização; não há entrada de UUID pelo usuário.
- Listagem de vínculos voltou a avaliar acesso atual à atividade/plano, e replay idempotente revalida acesso ao resultado antes de devolvê-lo.
- Plano encerrado e atividade em estado final não aceitam novas inclusões/vínculos.
- Fingerprint e persistência usam o mesmo conteúdo normalizado e não executam `Trim` inseguro em valores nulos.
- Backfill de comandos preenche `priority_id` apenas quando há exatamente uma prioridade vinculada; ambiguidades permanecem identificáveis por valor nulo.

### Verificações executadas

- `node --check backend/Valora.Web/wwwroot/js/executive-workspace.js`.
- `git diff --check`.
- Build .NET não executado neste contêiner porque o executável `dotnet` não está instalado.
- Testes PostgreSQL não executados porque `VALORA_TEST_POSTGRES_CONNECTION` não está configurada; nenhuma aprovação de banco é declarada.

### Pendente

- A consolidação do criador transacional compartilhado e os detalhes compostos de plano/atividade permanecem pendentes; não foi criada implementação paralela para encobrir essa lacuna.
- Homologação funcional autenticada e capturas responsivas dependem de identidade, API e PostgreSQL disponíveis.

## Arquivos relevantes e critérios de aceite

- `backend/Valora.Infrastructure/Repositories/PriorityActionRepository.cs`: nenhuma leitura UUID como inteiro; lock e transação presentes; contexto divergente conflita.
- `backend/database/postgresql/script_completo.sql`: schema canônico contém `priority_id`, backfill não apaga dados e comandos novos registram contexto.
- `backend/Valora.Api/Controllers/WorkspaceController.cs`: opções não expõem recursos de outro responsável a usuário restrito.
- `backend/Valora.Web/Views/Workspace/Index.cshtml` e `wwwroot/js/executive-workspace.js`: somente campos do percurso ativo são enviados e sucesso abre a atividade.
- `backend/Valora.Tests/PriorityActionContractTests.cs`: regressões estruturais permanecem cobertas.

## Sprint — histórico confiável e detalhes operacionais (2026-09-14)

### Implementação concluída

- As raw strings dos seletores foram corrigidas e o cálculo de `OFFSET` agora detecta overflow.
- Progresso, bloqueio e conclusão usam lock, versão otimista, transação única e registram o estado anterior real. Progresso comum aceita somente 0–99%; 100% é exclusivo da conclusão com resultado e evidência.
- Chaves de comando tornam histórico, check-in e conclusão idempotentes. Recursos fora da organização e transições finais não geram escrita.
- Indicadores são agregados sobre todo o escopo; o limite de 12 aplica-se somente à agenda. Canceladas e concluídas não entram nos indicadores operacionais; bloqueadas têm contador próprio.
- O detalhe da atividade passou a usar modelo composto tipado, nome de responsável e plano, versão, resultado/evidências, histórico paginado e diálogos acessíveis condicionados ao estado.
- O progresso do plano é operacional: média das atividades não canceladas; plano sem atividades aparece como 0%, não como maturidade metodológica.

### Homologação pendente e verificável

- Build .NET permanece pendente neste contêiner enquanto o SDK `dotnet` não estiver instalado.
- Testes de repository e jornada autenticada permanecem pendentes sem `VALORA_TEST_POSTGRES_CONNECTION`, identidade/API e navegador configurados. Isso não constitui aprovação funcional do PostgreSQL.
- Capturas autenticadas nos cinco viewports devem ser produzidas no ambiente de homologação após aplicar a migração aditiva de versão e idempotência.

## Sprint — consolidação comercial do ActionCenter (2026-09-14)

### Implementado neste incremento

- Materializações de plano, atividade, dashboard e detalhe agora projetam nomes, contagens, versão, título do plano e resultado de conclusão com tipos explícitos.
- O Web usa `ICurrentRequestContext`, exige organização selecionada para administrador global e aplica `action.read`, `action.manage` e `action.complete` nas fronteiras MVC.
- Listagens, indicadores, detalhes e histórico aplicam o mesmo escopo de recurso; atividades do plano e histórico possuem paginação estável.
- A idempotência serializa por organização/atividade/chave, compara autor, operação, payload normalizado e versão original, e rejeita reutilização divergente.
- Bloqueio possui retomada explícita para `in_progress`, preservando progresso, justificativa, autor, versão e chave.
- Falhas de comandos e criação voltam à mesma tela sem TempData sensível, preservam valores/chave, reabrem o diálogo e mostram resumo de validação.
- Scripts inline do módulo foram substituídos por `wwwroot/js/action-center.js`, com foco, retorno ao acionador e prevenção de duplo envio.

### Limites de homologação

A implementação não equivale à homologação. Build, PostgreSQL integrado, fluxo autenticado no navegador e capturas nos cinco viewports devem ser registrados abaixo somente quando executados no ambiente disponível.

## Sprint — execução consistente, criação segura e listagem comercial (2026-09-14)

### Implementado neste incremento

- O escopo explícito `data-action-center-module` passou a reunir conteúdo, acionadores e diálogos dos detalhes de plano e atividade. Todos os diálogos têm nome acessível, retorno de foco, validação dentro do modal, estado inicial para detecção real de alterações, confirmação antes de descarte e bloqueio de submissão dupla.
- A criação direta de atividade deixou de usar título como identidade. A chave de comando é serializada por organização, autor, operação e payload normalizado; o lock transacional protege concorrência, replay equivalente devolve o ID original e replay divergente exige uma nova intenção.
- A transação de criação revalida usuário ativo, módulo contratado, acesso de escrita ao plano, estado do plano, responsável, prazo segundo o fuso cadastrado da organização e referências de diagnóstico/resultado. O evento Journey e o comando idempotente pertencem à mesma transação.
- A listagem de planos passou a paginar e contar no PostgreSQL com busca, situação, responsável, prioridade, prazo e ordenação estável. O dashboard busca somente os seis planos necessários e calcula a quantidade de planos ativos separadamente.
- A consulta de responsáveis passou de um corte fixo em 200 para contrato paginado, pesquisável e autorizado, sem expor e-mail; `IncludeId` preserva uma seleção submetida fora da primeira página. O endpoint compartilhado do Workspace permite carregamento adicional pelo cliente.
- A migração canônica ganhou a tabela e os índices aditivos dos comandos de criação; atividades com títulos iguais continuam permitidas.

### Verificações e limites reais

- `node --check backend/Valora.Web/wwwroot/js/action-center.js` e `git diff --check` foram executados com sucesso.
- O SDK `dotnet` não está instalado neste contêiner; build e testes .NET não foram executados aqui.
- `VALORA_TEST_POSTGRES_CONNECTION` não está configurada; a migração e os cenários concorrentes não foram executados contra PostgreSQL, portanto não há declaração de aprovação do banco.
- Não havia ambiente autenticado do Web/API nem navegador conectado ao PostgreSQL para percorrer ou capturar os cinco viewports. A validação visual e o percurso completo permanecem tarefas de homologação, não aprovação implícita.

## Sprint — correção Razor e resiliência do ActionCenter (2026-09-14)

- Corrigida a colisão entre a variável local `page` e a diretiva Razor `@page` nas views MVC de atividades e detalhe do plano; ambas foram reformatadas sem convertê-las em Razor Pages.
- A paginação passou a expor página atual, total de páginas e disponibilidade de anterior/próxima no contrato compartilhado. As três navegações do escopo não geram mais links quando o destino não existe e os filtros são preservados por Tag Helpers.
- A listagem de atividades agora filtra, conta e pagina no PostgreSQL por texto, situação, prioridade, responsável e prazo, com parâmetros inválidos normalizados e ordenação estável.
- O formulário de atividade restaura também a prioridade enviada após validação/conflito. Os diálogos continuam identificados por `data-action-dialog`, nomeados, protegidos contra descarte e submissão duplicada.
- A criação autorizada/idempotente, política civil de prazo e seletor paginado de responsáveis permanecem na operação canônica introduzida no incremento anterior.

### Limitações de verificação

- O SDK `dotnet` não está instalado na imagem atual; restore, builds do Web/solução/testes e a confirmação executável do CS0006 não puderam ser realizados localmente.
- `VALORA_TEST_POSTGRES_CONNECTION` não está configurada; cenários concorrentes e filtros SQL não foram declarados como aprovados.
- Não existe ambiente autenticado com navegador e PostgreSQL nesta imagem para capturas dos cinco viewports.
