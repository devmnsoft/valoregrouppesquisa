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
