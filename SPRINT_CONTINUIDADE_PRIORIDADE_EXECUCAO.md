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
- A autorização de listagem foi aplicada na fronteira da API; migrar paginação e filtros para SQL será necessário para grandes volumes.
- `ItemDetails` e `PlanDetails` ainda precisam de modelos compostos tipados para histórico, nomes de responsáveis e conflito otimista.
- Formulários alcançáveis ainda não inspecionados nominalmente: Administração SaaS, Pessoas, Diagnósticos, Pesquisas, Inteligência, Jornada e Evolução. Não há declaração de padronização global.
- Homologação visual autenticada nos cinco viewports depende de ambiente com identidade, API e PostgreSQL configurados.

## Próximo incremento

1. Extrair criação transacional de plano/atividade para um serviço ActionCenter que aceite `IUnitOfWork` sem abrir transação aninhada.
2. Criar consultas paginadas no repositório incluindo nome do responsável e permissão efetiva do plano.
3. Separar check-in, bloqueio e conclusão em diálogos acessíveis compartilhados e adicionar histórico aos detalhes.
4. Executar fixture sintética de duas organizações e captura visual nos cinco viewports.

## Arquivos relevantes e critérios de aceite

- `backend/Valora.Infrastructure/Repositories/PriorityActionRepository.cs`: nenhuma leitura UUID como inteiro; lock e transação presentes; contexto divergente conflita.
- `backend/database/postgresql/script_completo.sql`: schema canônico contém `priority_id`, backfill não apaga dados e comandos novos registram contexto.
- `backend/Valora.Api/Controllers/WorkspaceController.cs`: opções não expõem recursos de outro responsável a usuário restrito.
- `backend/Valora.Web/Views/Workspace/Index.cshtml` e `wwwroot/js/executive-workspace.js`: somente campos do percurso ativo são enviados e sucesso abre a atividade.
- `backend/Valora.Tests/PriorityActionContractTests.cs`: regressões estruturais permanecem cobertas.
