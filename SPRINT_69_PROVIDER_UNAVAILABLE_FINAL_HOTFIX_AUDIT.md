# Sprint 69 — Auditoria do hotfix final provider_unavailable

1. `provider_unavailable` é lançado em `submitPublicSurveyAuto` somente depois de esgotar Cloud Functions, Firestore e API externa.
2. A função que envia a resposta pública é `submitPublicSurveyResponse`, delegando para `submitPublicSurveyAuto`.
3. Para pesquisa gratuita oficial, o submit não usa external-api primeiro; usa Cloud Functions, Firestore e API externa.
4. O submit tenta Cloud Functions de verdade via `callFirebaseFunction('submitSurveyResponse')` com SDK compat e caminho modular.
5. O submit tem fallback Firestore em `submitPublicSurveyViaFirestoreFallback`.
6. `window.ValoraFirebase.functions` existe em runtime quando Functions inicializa; falhas não quebram a SPA.
7. `callFirebaseFunction` funciona com SDK compat via `firebase.functions().httpsCallable`.
8. `callFirebaseFunction` funciona com SDK modular se `window.firebaseFunctionsModular.httpsCallable` estiver disponível.
9. `submitSurveyResponse` existe em `functions/index.js`.
10. `submitSurveyResponse` retorna `responseId` real do documento criado.
11. `submitSurveyResponse` retorna `resultToken` real gerado no servidor.
12. O erro real sanitizado da Cloud Function entra em `window.ValoraRuntimeDiagnostics.lastPublicSubmit.attempts`.
13. O erro real sanitizado da API externa entra em `window.ValoraRuntimeDiagnostics.lastPublicSubmit.attempts`.
14. O erro real sanitizado do Firestore fallback entra em `window.ValoraRuntimeDiagnostics.lastPublicSubmit.attempts`.
15. A pesquisa gratuita valida `publicToken`, `token` ou `accessToken`, nunca `tokenHash` vindo da URL.
16. `tokenHash` não deve aparecer na URL; validações rejeitam uso como token público.
17. `expiresAt` vencido não bloqueia pesquisa gratuita oficial não revogada.
18. O e-mail é tentado após submit por `sendResultEmailAuto`.
19. O certificado aparece na tela de resultado e tem botão de impressão/baixa.
20. Corrigido para não parar no primeiro provider: pesquisa gratuita agora tenta Cloud Functions, Firestore emergencial e API externa, retornando sucesso se qualquer provider entregar `responseId` e `resultToken`.

## Arquivos auditados
`config.js`, `index.html`, `app.js`, `firebase-init.js`, `firebase-repository.js`, `api-client.js`, `api-repository.js`, `gateway-client.js`, `runtime-capabilities.js`, `functions/index.js`, `functions/package.json`, `firebase.json`, `firestore.rules`, `scripts/build-production.js`, `package.json`, `tests/e2e/`.
