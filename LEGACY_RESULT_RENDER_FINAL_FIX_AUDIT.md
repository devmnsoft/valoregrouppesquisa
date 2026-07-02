# Auditoria final — correção definitiva do resultado legado

Arquivos auditados: `app.js`, `firebase-repository.js`, `repository.js`, `report-service.js`, `pdf.js`, `functions/index.js`, `style.css` e validadores `scripts/validate-*.js`.

1. `handlePublicSubmitSuccess(result, payload)` recebe o retorno normalizado do submit em `app.js`, logo após `submitPublicSurveyAuto`/`normalizePublicSubmitResult` concluir o envio público.
2. `renderResult` é chamado em rotas de resultado, em visualização administrativa e agora apenas como melhoria progressiva pós-submit por `tryEnhancePublicResult`, nunca como primeira tela após submit válido.
3. O erro real fica em `window.ValoraRuntimeDiagnostics.lastResultRenderError.originalMessage`, com `name`, `stack` truncado e `at`. Falhas da melhoria progressiva também ficam em `lastResultEnhanceError`.
4. `responseId` e `resultToken` são extraídos de `result.responseId || result.id` e `result.resultToken || result.accessToken`; quando presentes, a URL é atualizada com `?result=<id>&rt=<token>`.
5. `submitSurveyResponse` retorna objeto com `responseId`, `resultToken`/`accessToken`, `score`, `level`, possível `resultEmail` e dados parciais suficientes para primeira devolutiva.
6. `renderResult` historicamente esperava payload completo de `loadPublicResult`: `response`, `survey`, `company`, pontuações normalizadas, dimensões, comunicação, certificado e dados locais.
7. A divergência era o submit retornar um resultado parcial enquanto `renderResult` podia acessar dados de resultado completo/cache/estado local; agora `normalizeImmediateSubmitResultViewModel` e `normalizePublicResultViewModel` reduzem essa divergência.
8. `loadPublicResult` pode falhar por token, indisponibilidade de Function/API ou payload incompleto. A falha agora é não bloqueante em `tryEnhancePublicResult` e no botão `reloadPublicResult`.
9. Certificado e e-mail não podem mais derrubar a tela imediata: ações públicas usam loading, `try/catch`, mensagens controladas e preservam o CTA de WhatsApp.
10. A correção garante tela útil sempre porque `handlePublicSubmitSuccess` renderiza primeiro `renderImmediateResultAfterSubmit(result, payload)`, persiste snapshot em `sessionStorage`, registra diagnostics e só depois tenta carregar/renderizar o resultado completo como aprimoramento progressivo.
