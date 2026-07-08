# Auditoria final — motor de devolutiva Valora Insight™

1. A pesquisa pública Valora Insight™ é renderizada em `app.js` no fluxo de jornada pública e no formulário com `data-public-survey-form`.
2. As respostas são coletadas por `guardedPublicSurveySubmit`, `buildPublicSurveySubmitPayload` e `submitPublicSurveyResponse` em `app.js`.
3. A pontuação total é calculada em `calculateSurveyResult` e normalizada para 125 em `normalizeValoraInsightScores`.
4. A pontuação por dimensão é calculada em `calculateSurveyResult` (`byDimension`) e padronizada nas cinco dimensões oficiais em `normalizeValoraInsightScores`.
5. `renderResult` monta a devolutiva pública chamando `buildValoraInsightDevolutiva` e `renderValoraInsightResultPage`.
6. `renderImmediateResultAfterSubmit` monta a devolutiva inicial com o mesmo motor determinístico.
7. `getPublicResult` é chamado via `ValoraRepository.loadPublicResult`, usado por `tryEnhancePublicResult`, `renderResult` e ações públicas.
8. O 403 ocorria quando a rota pública tentava depender de autenticação ou token ausente; o fluxo correto usa `?result=<responseId>&rt=<resultToken>` e bypass público por token.
9. Relatório e certificado dependem de `loadPublicResultBundleForAction`, com fallback em `sessionStorage` após submit.
10. O cache pós-submit está em `valora:lastPublicResult`, lido por `readLastPublicResultFromSession`.
11. `Valora Pulse™` permanece como nome da plataforma; telas públicas usam `PUBLIC_PRODUCT_NAME` = `Valora Insight™`.
12. Ocorrências de `HOME` foram auditadas para navegação pública e devem aparecer como `Início`.
13. O texto “Pesquisa gratuita da Home: diagnóstico público” foi auditado para ser substituído por “Pesquisa gratuita”.
14. `formatPublicDate` evita `Invalid date` e retorna fallback seguro.
15. O card branco duplicado foi removido do fluxo usado: `renderValoraInsightResultPage` não chama `renderExecutiveSummaryCard`.
16. WhatsApp é criado por `publicWhatsappContactUrl`/`whatsappLink`, com número +55 91 99254-5353.
17. O e-mail decide status em Functions (`sent`, `queued`, `failed`) e a UI interpreta em `renderEmailDeliveryStatus`/`resendPublicResultEmailSafe`.
18. O link de resultado é montado com `?result=<responseId>&rt=<resultToken>` no fluxo transacional.
19. O prompt do PDF virou regra de negócio determinística: níveis oficiais, cinco dimensões, radar, benchmarking, verdade estratégica, risco, próximo nível e transição.
20. Correções feitas: motor oficial, render premium, relatório PDF contextual, certificado com Valora Insight™, data segura, cache/fallback, validadores e scripts npm.
