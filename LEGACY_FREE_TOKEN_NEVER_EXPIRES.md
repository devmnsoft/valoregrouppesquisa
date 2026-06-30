# LEGACY_FREE_TOKEN_NEVER_EXPIRES

Sprint 67 mantém a SPA legada em produção com Firebase Hosting, Cloud Functions v2 em Node 22 e Firebase Secret para SMTP.

## Deploy correto

- Functions: `npm run functions:deploy`
- Hosting: `npm run hosting:deploy`
- Fluxo completo Firebase: `npm run deploy:firebase`

## Segurança SMTP

A senha SMTP não deve ser commitada, documentada ou exibida em logs. Use somente o Firebase Secret `SMTP_PASSWORD`.

Se a senha SMTP foi compartilhada em chat, print, log ou repositório, rotacione imediatamente e configure a nova senha apenas com:

```bash
firebase functions:secrets:set SMTP_PASSWORD --project gestordepesquisa
```

## Validações Sprint 67

Execute `npm run security:no-secrets`, `npm run scripts:required`, `npm run functions:node22-readiness`, `npm run legacy:public-submit-flow`, `npm run legacy:result-email-send`, `npm run legacy:plans-tab`, `npm run legacy:free-token-never-expires` e `npm run hosting:dist-build` antes do deploy.
