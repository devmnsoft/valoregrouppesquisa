# Sprint 75 — Auditoria Functions Telegram Secret Optional

Data: 2026-06-30
Projeto Firebase: `gestordepesquisa`
Repositório: `devmnsoft/valoregrouppesquisa`

## Escopo auditado

- `functions/index.js`
- `functions/utils/telegram.js`
- `functions/utils/logging.js`
- `functions/utils/audit.js`
- `functions/package.json`
- `firebase.json`
- `package.json`
- `scripts/`
- `scripts/validate-functions-node22-deploy-readiness.js`
- `scripts/validate-functions-secrets-readiness.js`
- `scripts/validate-hosting-dist-build.js`

## Respostas objetivas

1. **Onde TELEGRAM_BOT_TOKEN é declarado?**  
   Não é mais declarado com `defineSecret` em `functions/index.js`. O nome aparece apenas como variável opcional lida em runtime por `process.env.TELEGRAM_BOT_TOKEN`, na documentação e no validador.

2. **Onde TELEGRAM_BOT_TOKEN é usado?**  
   Apenas em `functions/utils/telegram.js`, dentro de `sendTelegramNotification`, como leitura opcional quando `TELEGRAM_ENABLED=true`.

3. **Quais Functions incluem TELEGRAM_BOT_TOKEN em secrets?**  
   Nenhuma Function exportada inclui `TELEGRAM_BOT_TOKEN` em `secrets`.

4. **O Telegram é obrigatório para pesquisa pública?**  
   Não. `validateSurveyLink`, `submitSurveyResponse` e `getPublicResult` não possuem secret Telegram.

5. **O Telegram é obrigatório para e-mail?**  
   Não. As Functions de e-mail dependem de `SMTP_PASSWORD`, não de Telegram.

6. **O Telegram é obrigatório para cadastro de cliente?**  
   Não. `createClient` e `updateClient` não possuem secret Telegram.

7. **O Telegram é obrigatório para cadastro de usuário?**  
   Não. `createUser`, `updateUserProfile` e `sendUserInvite` não possuem secret Telegram.

8. **A ausência do Telegram pode bloquear deploy hoje?**  
   Não pelo código atual, porque não há `defineSecret('TELEGRAM_BOT_TOKEN')` nem `secrets:[TELEGRAM_BOT_TOKEN]` nas Functions.

9. **Como a correção tornou Telegram opcional?**  
   Removendo `defineSecret` de Telegram do `functions/index.js`, removendo Telegram da lista `secrets` das Functions `sendTelegramAlert` e `notifyCriticalError`, e centralizando o envio em `sendTelegramNotification`, que retorna `telegram_disabled` ou `telegram_not_configured` sem lançar erro.

10. **SMTP_PASSWORD continua obrigatório para e-mail?**  
    Sim. `SMTP_PASSWORD` permanece declarado com `defineSecret('SMTP_PASSWORD')`.

11. **As Functions que precisam de SMTP_PASSWORD continuam com secret?**  
    Sim. `createUser`, `sendUserInvite`, `sendSurveyInvitations`, `getEmailStatus`, `sendEmail`, `sendResultEmail`, `queueResultEmail`, `processEmailJob`, `retryEmailJob`, `resendResultEmail` e o agendador `scheduledProcessEmailRetries` continuam com `SMTP_PASSWORD` quando executam fluxo de e-mail.

12. **O deploy passa sem TELEGRAM_BOT_TOKEN?**  
    Sim quanto à validação estática de código: `functions:secrets-readiness` confirma que Telegram não está em `defineSecret` nem nos secrets críticos. O deploy real ainda depende de credenciais Firebase/CLI e do secret obrigatório `SMTP_PASSWORD` para e-mail.

13. **O deploy passa com TELEGRAM_BOT_TOKEN configurado?**  
    Sim. Se `TELEGRAM_ENABLED=true`, `TELEGRAM_BOT_TOKEN` e `TELEGRAM_CHAT_ID` estiverem disponíveis no ambiente, `sendTelegramNotification` tentará enviar. Se não estiverem, o fluxo retorna skip seguro.

14. **Quais scripts foram adicionados?**  
    - `scripts/validate-functions-secrets-readiness.js`
    - `functions:secrets-readiness` no `package.json`
    - `tools/windows/109-corrigir-secret-telegram-functions-deploy.bat`

15. **Quais comandos corretos devem ser usados para deploy?**  

```bash
npm run functions:install
npm run functions:node22-readiness
npm run functions:secrets-readiness
npm run functions:lint
firebase deploy --only functions --project gestordepesquisa
```

Ou o comando encadeado:

```bash
npm run functions:deploy
```

Para Hosting:

```bash
npm run build:prod
npm run hosting:dist-build
firebase deploy --only hosting --project gestordepesquisa
```

## Secrets e variáveis

### Obrigatório para e-mail

```bash
firebase functions:secrets:set SMTP_PASSWORD --project gestordepesquisa
```

### Opcional para Telegram

```bash
firebase functions:secrets:set TELEGRAM_BOT_TOKEN --project gestordepesquisa
```

Variáveis opcionais:

```bash
TELEGRAM_ENABLED=false
TELEGRAM_CHAT_ID=
```

## Resultado da auditoria

- Telegram não bloqueia deploy.
- SMTP permanece obrigatório para envio real de e-mail.
- Node runtime permanece `nodejs22` em `firebase.json` e `22` em `functions/package.json`.
- `firebase-functions` foi atualizado no manifesto para `^7.2.5` e `firebase-admin` para `^13.10.0` para preservar compatibilidade CommonJS/namespace do código atual.
- O script de dist aceita assets versionados e normaliza separadores Windows.
