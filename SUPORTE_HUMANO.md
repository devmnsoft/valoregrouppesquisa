# Suporte humano

Entidades:
- `supportConversations`: conversa, status, prioridade, empresa, solicitante, atendente, canal e metadados.
- `supportMessages`: mensagens da conversa, remetente, anexos futuros, flag interna e leitura.

Perfis com atendimento:
- `canHandleSupport`: Empresa Admin atende a própria empresa.
- `canHandleGlobalSupport`: Admin/Consultor Valora atendem globalmente.

Público externo deve usar Cloud Function pública com rate limit para criação segura em produção.
