# ValoraBot 2.0

## Arquitetura

O ValoraBot 2.0 é um assistente de produto baseado em regras, contexto e base de conhecimento interna. Ele não chama LLM externo no frontend e mantém `CHATBOT_MODE = 'rules'` para permitir evolução futura via Cloud Functions.

Componentes principais:

- `chatbot-knowledge-base.js`: base editorial por público/perfil, intenções textuais, rotas relacionadas e ações sugeridas.
- `chatbot-service.js`: contexto, detecção de intenção, seleção de conhecimento, respostas, histórico local e preparação de escalonamento.
- `manual-service.js`: manuais por perfil consultáveis pelo bot e pela UI.
- `app.js`: renderização da conversa, botões de ação, atendimento humano e painel de perguntas sem resposta.

API exposta em `window.ValoraChatbot`:

```js
{
  getChatContext,
  getKnowledgeBase,
  detectIntent,
  buildAnswer,
  getSuggestedQuestions,
  createBotMessage,
  createUserMessage,
  startConversation,
  sendMessage,
  escalateToHuman,
  getConversationHistory
}
```

## Intenções implementadas

`greeting`, `help`, `how_to_start`, `create_company`, `create_employee`, `choose_role`, `create_form`, `create_question`, `question_types`, `scoring`, `dimensions`, `create_survey`, `send_invites`, `answer_survey`, `lgpd`, `view_results`, `reports`, `dashboard`, `action_plan`, `notifications`, `billing`, `plans`, `white_label`, `logs`, `telegram`, `integrations`, `support_human`, `error_help` e `unknown`.

## Contexto usado nas respostas

O bot coleta login, perfil, empresa, rota, plano, módulos habilitados, status de dados operacionais, jornada pública e ambiente (`local_demo` ou `firebase`). As respostas aplicam restrições de perfil, por exemplo Participante não recebe instrução para criar pesquisa; Analista de Resultados é orientado a relatórios e filtros, não edição.

## Ações sugeridas

As respostas podem retornar botões:

- `route`: navega para uma rota interna.
- `manual`: abre o manual do perfil.
- `support`: abre atendimento humano.

## Logs e LGPD

O histórico local registra somente conteúdo necessário da conversa em `state.chatbotConversations`. Perguntas sem resposta entram em `state.chatbotUnansweredQuestions` e aparecem no painel Admin Valora em Logs. Não há envio para serviço externo nem chave de IA no frontend.

## Evolução futura com IA real

Para usar IA generativa, mantenha o frontend sem chave. A chamada deve passar por Cloud Function, com minimização de dados, consentimento quando necessário, filtros por perfil/empresa, fallback para regras e auditoria em logs.
