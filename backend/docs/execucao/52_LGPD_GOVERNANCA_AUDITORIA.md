# LGPD, Governança e Auditoria — execução 52

- Adicionados aliases administrativos `/api/v1/privacy/requests` para listar, criar e atualizar solicitações usando o serviço LGPD existente.
- A organização do token substitui qualquer organização enviada no corpo da criação autenticada.
- O BFF encaminha privacidade, governança e auditoria sem expor o access token ao navegador.
- O schema de privacy request foi complementado com máscara, protocolo canônico, resposta, responsáveis, conclusão e metadata.
- Audit recebeu `ip_hash`, `user_agent` e soft delete; governança conserva before/after, justificativa, severidade e correlation id.

Nenhum e-mail puro, token, senha ou secret foi adicionado às respostas administrativas.
