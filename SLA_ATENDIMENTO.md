# SLA de Atendimento — Valora Pulse

Políticas padrão (`supportSlaPolicies`):

| Prioridade | Primeira resposta | Resolução |
|---|---:|---:|
| Baixa | 24h | 72h |
| Média | 8h | 48h |
| Alta | 2h | 24h |
| Crítica | 30min | 8h |

O sistema calcula `slaDueAt` pelo prazo de resolução e exibe: dentro do prazo, próximo do vencimento ou vencido. A função agendada `checkSupportSla` registra vencimentos para auditoria e notificação.
