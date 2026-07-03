# Auditoria final — Resultado público, e-mail e certificado legado

## Arquivos auditados
- `app.js`, `firebase-repository.js`, `repository.js`, `functions/index.js`, `pdf.js`, `report-service.js`, `config.js`, `config/config.production.js`, `scripts/build-production.js` e validadores em `scripts/validate-*.js`.

## Achados e correções
1. `renderResult` montava o certificado com `safeCertificateHtml(r,s,formForInsight,company)` sem garantir `company` no escopo público. Corrigido com `normalizePublicResultBundle` e declaração explícita de `company`.
2. `renderResult` agora aceita o payload completo opcional (`resultOrPayload`) e normaliza `response`, `survey`, `form`, `company`, `score`, `level`, `responseId` e `resultToken`.
3. `submitSurveyResponse` retorna o resultado imediato com `responseId`, `resultToken`, `score/level`, participante e `resultEmail.status` quando aplicável.
4. `getPublicResult`/`loadPublicResult` retorna bundle público com `response/result`, `survey`, `company` e dados de certificado; o front passa esse bundle a `renderResult`.
5. `tryEnhancePublicResult` chama `renderResult(responseId, true, resultToken, full)` dentro de `try/catch`, registrando `lastResultEnhanceError` sem derrubar a tela.
6. `reloadPublicResult` chama `tryEnhancePublicResult` dentro de `withLoading` e não propaga Promise rejeitada ao usuário.
7. `viewResponse` usa `safeRenderResultById`, registra `lastViewResponseError` e cai para resultado em cache/fallback.
8. A chamada de login Firebase existe somente no fluxo de login (`repository.login`/`firebase-repository.login`). A rota pública `?result=&rt=` agora usa `isPublicResultRoute()` e `renderPublicResultFromRoute()` antes de qualquer tela de login.
9. O fluxo correto posterior usa `resultToken` público via `ValoraRepository.loadPublicResult(responseId, token)` e Cloud Function `getPublicResult`.
10. `sendResultEmail` lê SMTP por secrets/env através de `emailConfig()`/`assertEmailConfigReady()` em `functions/index.js`.
11. Há criação de fila em coleções legadas `email_jobs` no fallback do front e `emailJobs` nas Functions. `sendResultEmail` registra `emailJobs` com status `pending`, `sent` ou `pending_retry`.
12. Certificado PDF/PNG passava por `buildCertificateData`; agora PDF usa `safeBuildCertificateData` e PNG retorna aviso controlado quando indisponível.
13. ReferenceErrors recentes mitigados: `recommendationFor` recebe fallback seguro no view model; `dimensionRecommendation` fica encapsulado por helpers seguros de certificado; `company` é declarado no bundle de `renderResult`.
14. Correções aplicadas: normalizadores, rota pública sem Auth, e-mail por secrets SMTP/Nodemailer, certificado PDF seguro, safeRun para Promises já validado, timeout de loading e validadores de regressão.
15. Testes feitos: ver seção Testing da resposta final com comandos e resultados.
