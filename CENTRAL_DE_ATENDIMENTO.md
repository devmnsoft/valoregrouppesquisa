# Central de Atendimento, Tickets e SLA — Valora Pulse

A Central de Atendimento adiciona tickets estruturados ao Valora Pulse sem remover o ValoraBot. O bot continua como primeiro nível; quando a resposta não resolve ou o usuário pede atendente, o sistema abre um ticket com contexto de perfil, empresa, rota e pesquisa pública quando existir.

## Coleções

- `supportTickets`: ticket principal com empresa, solicitante, categoria, prioridade, status, origem, canal, vínculos de pesquisa/resposta, SLA, leitura, avaliação e auditoria.
- `supportTickets/{ticketId}/messages`: conversa do ticket com mensagens visíveis e notas internas (`internal=true`).
- Compatibilidade local/demo: o estado local mantém `supportTickets`, alias `supportConversations`, `supportMessages`, `supportCategories`, `supportSlaPolicies` e `knowledgeBase`.

## Perfis

- Admin Valora: vê, assume, transfere, resolve, encerra e acompanha todos os tickets.
- Consultor Valora: vê e atua em tickets globais conforme permissão.
- Empresa Admin e atendentes da empresa: veem tickets da própria empresa, assumem e respondem.
- Gestor de Pesquisa: abre tickets e acompanha tickets relacionados à empresa/pesquisa.
- Analista, Gestor Área e Participante: abrem e acompanham tickets próprios, respeitando escopo.
- Público sem login: abre ticket por formulário/Cloud Function com rate limit; não lista tickets.

## Fluxos

1. Usuário clica **Falar com atendente** no ValoraBot, manual ou portal.
2. Logado: o sistema usa nome, e-mail, perfil e empresa; solicita categoria, prioridade, assunto e mensagem.
3. Público: solicita nome, e-mail, categoria, assunto e mensagem; se vier de pesquisa pública, vincula empresa, `surveyId` e rota.
4. `routeSupportTicket(ticket)` sugere atendimento da empresa, fila Valora, financeiro ou técnico.
5. Atendente assume, responde, cria nota interna, resolve ou encerra.
6. Solicitante avalia de 1 a 5 após encerramento.

## Segurança e LGPD

- Não há token Telegram no frontend.
- Mensagens internas não são exibidas ao solicitante.
- Empresa só acessa tickets da própria empresa.
- Participante só acessa tickets próprios.
- Público cria via backend/Cloud Function com validação, sanitização e rate limit.
- Alertas Telegram devem enviar metadados mínimos, sem conversa completa.
