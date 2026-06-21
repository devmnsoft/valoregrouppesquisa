# Alertas Telegram — Valora Pulse

## Princípio
O Valora Pulse registra internamente os eventos necessários, mas envia ao Telegram somente alertas relevantes para reduzir ruído, custo e risco LGPD.

## Secrets
Configure no Firebase Functions:

```bash
firebase functions:secrets:set TELEGRAM_BOT_TOKEN
firebase functions:secrets:set TELEGRAM_CHAT_ID
```

Nunca coloque token no frontend, `config.js`, `firebaseConfig`, localStorage ou tela administrativa.

## Função
`sendTelegramAlert` é uma callable Function. Testes manuais exigem `admin_valora`, sanitizam o payload, aplicam rate limit simples por `level + category + action + companyId` e registram sucesso/falha.

## Envio padrão
Enviar por padrão: erros críticos, segurança, falhas Firebase/Functions, webhook, integrações, billing crítico e testes manuais do admin.

Não enviar por padrão: debug, info, cliques, navegação comum, logs massivos ou dados pessoais sensíveis.

## Anti-spam
Há limite de 10 alertas a cada 5 minutos por chave de evento. Eventos excedentes são suprimidos e registrados como `telegram_rate_limited`.

## Mascaramento
E-mail vira formato `ma***@empresa.com`; documentos, tokens, secrets e API keys viram valores mascarados. Stack completa não é enviada ao Telegram.

## Desativar
No frontend/local, mantenha `observability.telegramEnabled: false` em `config.js` ou no documento restrito de configurações administrativas em produção.
