# Auditoria final — redesign da devolutiva pública legada

1. A devolutiva pública é renderizada em `app.js` por `renderResult`, `renderImmediateResultAfterSubmit` e pelo novo `renderPremiumPublicResult`.
2. `renderResult` é chamado na rota pública `?result=<responseId>&rt=<token>`, em `tryEnhancePublicResult`, em `renderPublicResultFromRoute` e em visualizações de resposta.
3. `renderImmediateResultAfterSubmit` é chamado por `handlePublicSubmitSuccess` logo após `submitSurveyResponse`, e por fallbacks controlados quando o resultado completo falha.
4. `getPublicResult` é chamado pelo provider em `firebase-repository.js`/`repository.js` via `ValoraRepository.loadPublicResult`, e por `getPublicResultAuto` em `app.js`.
5. O 403 ocorria quando o token seguro estava ausente/incorreto ou quando o fluxo tentava buscar o resultado sem fallback local. A Function agora valida somente `sha256(resultToken)` contra `resultTokenHash` e não exige `req.auth`.
6. O relatório dependia de `ValoraRepository.loadPublicResult` dentro de `reportResponsePdf`.
7. O relatório agora usa `loadPublicResultBundleForAction`, que primeiro tenta `sessionStorage['valora:lastPublicResult']` e cai para `getPublicResult` apenas quando necessário.
8. O certificado é montado por `safeBuildCertificateData`, `certificatePdf`, `downloadCertificatePdf` e `pdf.js:createCertificate`.
9. WhatsApp é criado por `publicWhatsappContactUrl`, `whatsappLink` e `openWhatsapp`, usando `https://wa.me/5591992545353`.
10. `Valora Pulse™` permanece permitido em telas administrativas; a devolutiva/certificado/relatório públicos usam `Valora Insight™`.
11. O menu público usa `Início`; referências administrativas legadas a HOME não compõem a devolutiva pública.
12. Datas públicas passam por `formatPublicDate`, evitando `Invalid date`.
13. “Pesquisa gratuita da Home: diagnóstico público” foi removido do código público.
14. O card branco duplicado “Enquadramento geral sem adoçamento” foi removido; a seção secundária é “Leitura executiva da realidade” quando aplicável.
15. E-mail retorna `sent`, `queued` ou `failed_non_blocking` com `errorCode`/`errorMessage`; o front não mostra sucesso para fila/falha.
16. O provider principal é `EMAIL_PROVIDER=http_api`, configurado por secrets `EMAIL_API_URL`, `EMAIL_API_KEY`, `EMAIL_FROM_EMAIL` e `EMAIL_FROM_NAME`, com SMTP como fallback.
17. Correções aplicadas: UX premium, contraste, mobile-first sem overflow, fallback por cache, relatório resiliente, certificado A4 paisagem premium, WhatsApp em `<a href>`, allowlist `legacy_run`, ação vazia ignorada, HTTP API de e-mail e validadores.
