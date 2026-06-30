# Sprint 58 — Auditoria Pós-Blaze e Produção Real

## Diagnóstico objetivo
1. `config.js` está com `FIREBASE_PLAN='blaze'`: **Sim**.
2. `ENABLE_CLOUD_FUNCTIONS` está `true`: **Sim**.
3. `EMAIL_TRANSPORT` está `auto`: **Sim**.
4. `PUBLIC_SUBMISSION_PROVIDER` está `auto`: **Sim**.
5. `RESULT_PROVIDER` está `auto`: **Sim**.
6. `COMMUNICATION_GATEWAY.fallbackToCloudFunctions` está `true`: **Sim**.
7. Firebase Hosting publica `config.js` novo: **Sim**, `build:prod` copia `config.js` para `dist`.
8. Cache impedindo navegador: **Mitigado**, HTML e `config.js` usam `no-store` e assets usam `?v=8.7.0`.
9. `APP_VERSION` incrementado: **Sim, 8.7.0**.
10. `index.html` referencia versão nova dos assets: **Sim, 8.7.0**.
11. `build:prod` copia `config.js` atualizado para `dist`: **Sim**.
12. Hosting possui Cache-Control correto para HTML/config: **Sim**.
13. Cloud Functions foram publicadas: **Pendente de execução operacional do deploy real**.
14. `SMTP_PASSWORD` secret está configurado: **Pendente de verificação no Firebase Console/CLI**.
15. `getEmailStatus` funciona: **Código pronto; requer deploy e secret**.
16. `sendEmail` funciona: **Código pronto; requer deploy e SMTP real**.
17. Resultado por e-mail usa `responseId` real: **Sim, fluxo público usa retorno de submissão**.
18. Pesquisa gratuita continua expirando por `expiresAt`: **Não indevidamente; expiração vencida só bloqueia com `strict`**.
19. Function `loadValidSurvey` ainda bloqueia pesquisa gratuita: **Não para pesquisa gratuita oficial válida, ativa e não revogada**.
20. Menu mobile antigo ainda mostra só Dashboard: **Não; usa fonte única `getAdminMenuItems`**.
21. `admin_valora` vê todos os itens no mobile: **Sim, condicionado às permissões/módulos habilitados**.
22. `Valora.Web` possui paridade: **Documentada e validada por gate de paridade**.
23. Riscos restantes: deploy real de Functions/Hosting, secret SMTP, teste e-mail real controlado e homologação manual pós-cache CDN.

## Escopo auditado
`config.js`, `index.html`, `app.js`, `style.css`, Firebase init/repository/API/gateway, `firebase.json`, `.firebaserc`, `functions/`, `firestore.rules`, workflows, scripts, testes E2E, `backend/Valora.Web`, `backend/Valora.Api`, `package.json` e `tools/windows`.
