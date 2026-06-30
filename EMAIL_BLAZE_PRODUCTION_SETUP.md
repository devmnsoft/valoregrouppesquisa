# Setup SMTP em produção com Firebase Blaze

1. Confirme no Console Firebase que o projeto `gestordepesquisa` está no plano Blaze.
2. Configure a senha SMTP como secret: `firebase functions:secrets:set SMTP_PASSWORD --project gestordepesquisa`.
3. Configure variáveis de ambiente das Functions: `SMTP_HOST`, `SMTP_PORT`, `SMTP_SECURITY`, `SMTP_USERNAME`, `SMTP_SENDER_EMAIL`, `SMTP_SENDER_NAME`.
4. Publique Functions: `firebase deploy --only functions --project gestordepesquisa`.
5. Publique Hosting: `firebase deploy --only hosting --project gestordepesquisa`.
6. Teste `getEmailStatus` no painel Diagnóstico do Ambiente ou via callable autenticada.
7. Teste `sendEmail` com template `test`, um único destinatário e usuário autorizado.
8. Verifique logs com `firebase functions:log --project gestordepesquisa --only sendEmail`.
9. Diagnostique SMTP validando host, porta, TLS/STARTTLS, usuário, remetente autorizado e rejeições 4xx/5xx.
10. Diagnostique CSP/API conferindo erros de navegador, CORS, `connect-src`, status 404/5xx e fallback para Cloud Functions.
11. Valide SPF, DKIM e DMARC no DNS do domínio remetente antes de ativar disparos reais.
