# Auditoria final — WhatsApp, pesquisa, resultado, mobile e PDF

1. Link da pesquisa pública: `functions/index.js` em `buildPublicSurveyUrl`, chamado por `preparePublicSurveyLink`.
2. Token público da pesquisa: `createTokenPair()` em `preparePublicSurveyLink`; o token real retorna ao admin, o hash fica em `publicTokenHash`/`tokenHash`.
3. Validação da pesquisa: `loadValidSurvey` valida `surveyId + token` com `verifyBearerToken(publicTokenHash, token)`.
4. Expiração no celular: links antigos reaproveitavam token real indisponível/expirado ou campos `expiresAt` heterogêneos; agora o compartilhamento sempre renova por 180 dias e a validação aceita Timestamp, Date, ISO e número no servidor.
5. Link do resultado: `buildPublicResultUrl(responseId, resultToken)`.
6. `resultToken`: `rotateResultTokenForResponse` e submissão pública criam token real com `createTokenPair`/`createResultAccessPair`.
7. `resultTokenHash`: salvo em `responses/{responseId}.resultTokenHash`.
8. WhatsApp de pesquisa: `app.js` em `shareSurveyWhatsapp`, via callable `preparePublicSurveyLink`.
9. WhatsApp de resultado: `app.js` em `shareResultWhatsapp`, via `adminCreateResultShareLink` no admin.
10. WhatsApp usando hash: validadores bloqueiam `publicTokenHash/tokenHash` na URL; implementação usa token real retornado pela Function.
11. Relatório/certificado admin: `adminReportResponsePdf` e `adminCertificatePdf`.
12. Admin PDF não usa `getPublicResult`: usa `adminLoadResponseBundle`/`adminGetResponseResult` ou estado local autenticado.
13. Quebras mobile: home/pesquisa/resultado/admin eram afetados por tabelas e grids sem empilhamento; corrigido em `style.css`.
14. CSS fixo/overflow: tabelas agora usam wrappers com `overflow-x:auto`; cards admin viram lista mobile.
15. Unicode PDF incompatível: removido `™` de `pdf.js`/`report-service.js`; radar PDF usa ASCII.
16. Radar `?????`: mitigado com `radarBarPdfSafe(score,max)` retornando `[#####-----]`.
17. Template mobile relatório/certificado: PDF A4/paisagem permanece em `pdf.js`, com texto WinAnsi seguro e quebra de linha.
18. Correções aplicadas: tokens canônicos, callables admin, WhatsApp seguro, validação pública robusta, CSS mobile-first, cards mobile, PDF seguro e validadores obrigatórios.
