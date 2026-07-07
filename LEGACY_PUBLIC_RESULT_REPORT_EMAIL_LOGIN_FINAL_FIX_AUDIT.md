# Auditoria final — resultado público, relatório, certificado, e-mail e login

1. **Resposta pública salva:** `functions/index.js`, `submitSurveyResponse`, grava em `responses/{responseId}` dentro de transação.
2. **`resultToken` gerado:** `functions/index.js`, `submitSurveyResponse`, com `createToken(32)` antes de salvar a resposta.
3. **`resultTokenHash` salvo:** `functions/index.js`, `submitSurveyResponse`, grava `resultTokenHash`, `resultTokenCreatedAt` e `resultAccessEnabled:true`.
4. **`getPublicResult` implementado:** `functions/index.js`, callable pública `exports.getPublicResult`.
5. **Causa do 403:** o token ausente/inválido era tratado como `permission-denied`; links sem `rt`, hashes faltantes e front que dependia apenas da Function faziam relatório/certificado falhar. A função agora aceita aliases, diferencia `result_token_hash_missing`, e nunca exige `req.auth`.
6. **`loadPublicResult` chama `getPublicResult`:** `firebase-repository.js`, `loadPublicResultFirebase`, chamando `callFunction('getPublicResult',{responseId,resultToken})` sem Auth.
7. **Relatório dependia de `getPublicResult`:** `app.js`, `reportResponsePdf`; corrigido para usar `loadPublicResultBundleForAction` com cache/sessionStorage.
8. **Certificado dependia de `getPublicResult`:** `app.js`, `certificatePdf`; corrigido para usar o mesmo bundle com fallback de cache.
9. **Cache/sessionStorage:** `app.js`, `readLastPublicResultFromSession` lê `valora:lastPublicResult`; fluxo de submit mantém diagnóstico em `ValoraRuntimeDiagnostics`/cache local.
10. **Tela “Resultado em processamento”:** `app.js`, `renderResultLoadFallback`; agora exibe código técnico, WhatsApp e tenta renderizar cache após 6 segundos.
11. **WhatsApp renderizado:** `app.js`, `publicWhatsappContactUrl` e `whatsappLink`, sempre `https://wa.me/5591992545353`.
12. **`action` vazia / `legacy_run`:** `app.js`, `legacyRun` usa allowlist `LEGACY_PUBLIC_ACTIONS`; ações vazias são ignoradas/avisadas sem quebrar.
13. **Entrar/login:** `app.js`, `renderLogin`, `handleLoginSubmit`, `createActions().login`.
14. **Mistura auth/public route:** `app.js`, `getPublicRouteParams`, `isAnyPublicRoute` e `routeFromLocation` desviam rotas públicas antes do login privado.
15. **Valora Pulse™ em tela pública:** constantes públicas em `app.js` usam `PUBLIC_PRODUCT_NAME='Valora Insight™'`; usos administrativos permanecem plataforma.
16. **HOME:** textos públicos revisados para “Início”.
17. **Invalid date:** `app.js`, `formatPublicDate`; PDF/certificado usam fallback “Data não informada”.
18. **Card branco duplicado:** texto “Enquadramento geral sem adoçamento” bloqueado por validador e ausente da devolutiva pública.
19. **E-mail:** `functions/index.js`, `sendResultEmailInternal` usa HTTP API preferencial e SMTP fallback, gravando `email_logs` e status honesto.
20. **Correções feitas:** resultado público por token sem Auth, token obrigatório no submit, fallback de relatório/certificado por cache, WhatsApp direto, CSS mobile-first premium, login robusto, validadores e auditoria final.
