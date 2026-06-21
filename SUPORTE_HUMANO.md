# Suporte humano

O suporte humano é integrado ao ValoraBot e ao portal.

## Quando o bot oferece atendimento

- Pergunta desconhecida ou baixa confiança.
- Pedido explícito por atendente, humano ou suporte.
- Erro, travamento, link expirado ou problema em pesquisa pública.
- Perfil sem permissão que precisa acionar administrador ou equipe Valora.

## Fluxo

1. O usuário clica em **Falar com atendente** ou digita uma frase gatilho.
2. Usuários logados abrem atendimento com nome, e-mail, perfil, empresa e rota atual.
3. Usuários sem login preenchem nome, e-mail, assunto e mensagem.
4. A conversa é criada em `supportConversations` e as mensagens em `supportMessages`.
5. Atendentes autorizados visualizam por escopo global ou empresa.

## Segurança

- O chat não deve solicitar senha.
- O contexto é operacional, não um dump de dados pessoais.
- Conversas públicas só são visíveis ao visitante sem usuário quando originadas como públicas; atendentes seguem permissões.
