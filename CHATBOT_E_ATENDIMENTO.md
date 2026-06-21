# Chatbot e atendimento

`chatbot-service.js` monta contexto por login, perfil, empresa e rota. Ele responde dúvidas públicas sem revelar dados internos, usa manuais como base de conhecimento e sugere perguntas rápidas por contexto.

Gatilhos como “falar com atendente”, “humano”, “suporte” e “não resolveu” escalam para atendimento humano.

O atendimento local/demo usa `supportConversations` e `supportMessages` no estado local. Em Firebase, as regras e Cloud Functions preparam criação pública segura com rate limit.
