# Auditoria objetiva — jornada de revisão de insights

## Base e limitações verificadas

- HEAD inicial reconfirmado: `ea040cc1ca8b842778e12393e0c74c49409094f3` (merge da PR #594), sem alterações locais.
- O checkout não contém PDFs. Nenhuma fórmula, peso ou regra metodológica foi criada para substituir documentos ausentes.
- O container não possui `dotnet`, `psql`, PostgreSQL nem navegador configurado. Por isso não há declaração de homologação, execução autenticada ou evidência visual nesta alteração.

## Matriz curta

| Funcionalidade | Implementação encontrada | Lacuna confirmada | Arquivos/validação desta alteração |
|---|---|---|---|
| Insight exibido no workspace | `DiagnosticWorkspaceRepository` lê `insights`, produzido pelo pipeline | Esse modelo técnico não é a fila editorial | Contrato preservado; nenhuma aprovação foi adicionada ao modelo técnico |
| Central `/Insights` | Usa `valora_ai_insights`, `AiReviewService` e páginas Razor | Revisão era composta por três gravações sem transação, sem versão e sem idempotência | Contratos, serviço, repositório, controller e `Details.cshtml`; testes unitários e estáticos |
| Decisão humana | `status`, revisor e data no próprio insight | Retry/concorrência podia gerar estado parcial ou sobrescrever decisão | `review_version`, atualização condicional e ledger de comandos na mesma transação |
| Justificativa | Feedback separado para rejeição | Falha entre feedback/fila/status deixava auditoria inconsistente | Motivo, fila, feedback, ledger e decisão persistidos atomicamente |
| Notificações | Índice único por organização/evento/tipo | Segundo destinatário era bloqueado e leitura não representava uma entrega independente | Reconciliação sem exclusão e índice por evento **e destinatário**; `resolved_at` separado de `read_at` |
| Upgrade legado | Índices diretos | Duplicatas antigas podiam impedir a criação | Duplicatas por destinatário são diagnosticadas na auditoria de convergência e preservadas como legado fora da chave ativa |

## Consumidores e decisão canônica

Há dois modelos diferentes. `insights` é a saída técnica versionada por execução usada pelo workspace diagnóstico e pelas projeções. A jornada humana existente em `/Insights` usa `valora_ai_insights`, ligado a `valora_ai_runs`, resultado e diagnóstico. A alteração não funde modelos incompatíveis nem passa a tratar um resultado técnico como aprovação editorial.

## Gate e backlog honesto

O código agora expressa atomicidade, idempotência e concorrência otimista da revisão, mas o gate de PostgreSQL só poderá ser aprovado após instalação limpa, reaplicação e upgrade representativo em uma instância real. Permanecem pendentes: fila com filtros/paginação completos, atribuição a usuário ativo, conversão assistida com vínculo à versão, agregação de planos/entregáveis no workspace, comparação metodológica entre ciclos, smoke autenticado com dois tenants e validação visual em 1440/768/390 px.
