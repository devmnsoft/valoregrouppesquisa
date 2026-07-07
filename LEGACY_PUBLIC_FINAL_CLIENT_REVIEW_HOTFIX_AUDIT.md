# Auditoria — hotfix final público legado

1. Home pública: `renderHome()` em `app.js` renderiza hero, CTA da pesquisa gratuita e WhatsApp.
2. Devolutiva pública: `renderPublicResultFromRoute()`, `renderResult()` e `renderImmediateResultAfterSubmit()` em `app.js`.
3. WhatsApp: helper único `publicWhatsappContactUrl()`, `whatsappLink()` e `openWhatsapp()` em `app.js`, usando `wa.me/5591992545353`.
4. `data-action` vazio: tratado em `handleDocumentClick()` com `normalizeActionName()` e `lastEmptyActionClick`; templates públicos foram protegidos para links com `href`.
5. `legacy_run`: roteado por `legacyRun()` e allowlist `LEGACY_PUBLIC_ACTIONS` em `app.js`.
6. `getPublicResult`: chamado por `loadPublicResultFirebase()` em `firebase-repository.js` e por `reportResponsePdf()`/resultado público em `app.js`.
7. 403 de `getPublicResult`: acontecia por rota/validação pública incompleta e por contrato não estritamente público por token; agora a Function valida `responseId` + `resultToken` e não exige `req.auth`.
8. `loadProfile`/Firebase Auth em rota pública: o boot `init()` agora retorna antes de `waitUntilReady()` quando `isAnyPublicTokenRoute()` é verdadeiro.
9. “Usuário inativo”: vem do fluxo autenticado/perfil; rotas `?survey=&token=` e `?result=&rt=` não entram mais nesse fluxo.
10. “Valora Pulse™” público: constantes separam `PUBLIC_PRODUCT_NAME` (`Valora Insight™`) e `PLATFORM_NAME` (`Valora Pulse™`); telas públicas usam o produto.
11. “HOME”: textos públicos usam “Início”.
12. “Pesquisa gratuita da Home: diagnóstico público”: substituída por “Pesquisa gratuita”.
13. “Invalid date”: `formatPublicDate()` centraliza fallback “Data não informada”.
14. Card branco duplicado: substituído por `renderExecutiveSummaryCard()` com card único escuro.
15. `reportResponsePdf`: implementado em `app.js`, carrega resultado público e chama `ValoraPdf.createReport`.
16. `certificatePdf`/`downloadCertificatePdf`: mapeados em `createActions()` e allowlist legada.
17. `resultEmail.status`: definido em `submitSurveyResponse`/`sendResultEmail` em `functions/index.js`.
18. `email_logs`: gravado por `writeEmailLog()` em `functions/index.js` para envio real, falha e teste SMTP.
19. Correções feitas: bypass público de Auth, link incompleto sem login, WhatsApp `wa.me`, clique vazio ignorado, `legacy_run` por allowlist, relatório PDF, layout mobile, SMTP Gmail 587, erro de e-mail visível e validadores.
