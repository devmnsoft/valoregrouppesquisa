# Auditoria final — fluxo legado de submit de pesquisa pública

## Escopo auditado
- `app.js`: state machine pública, renderização do formulário, montagem do payload, submit, loading, erro público, resultado e retry.
- `firebase-repository.js`: provider Firebase que chama `validateSurveyLink`, `submitSurveyResponse`, `getPublicResult` e `sendResultEmail`.
- `repository.js`: roteamento do repositório público para Firebase em operações públicas.
- `functions/index.js`: callable v2 `submitSurveyResponse`, `getPublicResult` e fila de e-mail de resultado.
- `style.css`: overlay global e spinner de loading.
- `scripts/build-production.js` e `scripts/validate-*.js`: gates estáticos de produção e validadores de regressão.

## 1. Onde `renderTakeSurvey` lê `sid`/`token`/`org`
`renderTakeSurvey(sid=null, token=null, orgSlug='', resolvedPayload=null)` lê os argumentos explícitos e faz fallback para `getPublicSurveyRouteParams()` antes de validar o contrato do link. Depois da validação remota, substitui `sid`, `token` e `orgSlug` pelos valores normalizados de `window.ValoraPublicSurveyState.context`.

## 2. Onde `validatePublicSurveyLink` retorna `survey`/`form`
`resolvePublicSurveyContext()` chama `validatePublicSurveyLink({ surveyId, token, org })`. Esse helper usa `window.ValoraRepository.validatePublicSurvey` quando disponível e, no fallback local, `validatePublicSurveyLinkLocally` retorna `{ ok:true, survey, form, company, lgpd }`.

## 3. Onde `setPublicSurveyContext` salva contexto
`setPublicSurveyContext(context)` normaliza `surveyId`, `token`, `org`, `survey`, `form`, `company` e `lgpd`, salva em `window.ValoraPublicSurveyContext`, atualiza `window.ValoraPublicSurveyState` para `ready` apenas quando há contexto mínimo, e grava diagnóstico em `window.ValoraRuntimeDiagnostics.lastPublicSurveyContext`.

## 4. Onde `renderPublicSurveyUnavailable` limpa a tela
`renderPublicSurveyUnavailable(error)` agora força `status:'unavailable'`, `context:null`, limpa `publicSurveyCache`, remove artefatos de formulário com `clearPublicSurveyDomArtifacts('render_unavailable')` e substitui o conteúdo inteiro de `#app` pela tela de indisponibilidade. Há assert runtime para detectar qualquer `[data-public-survey-form]` remanescente.

## 5. Onde o form `data-form="takeSurvey"` é renderizado
O HTML do formulário público é gerado em `renderTakeSurvey`, dentro de `.public-survey-section`, com `data-form="takeSurvey"`, `data-public-survey-form`, `data-survey-id`, `data-token-present` e `data-org`.

## 6. Onde `submitSurvey` monta payload
`submitSurvey(form)` chama `buildPublicSurveySubmitPayload(form)`, valida com `validatePublicSubmitPayload(submitPayload, getPublicSurveyContext(form), form)`, aplica uma guarda extra para `surveyId`/`token`, registra diagnóstico e somente então chama `submitPublicSurveyResponse(submitPayload)`.

## 7. Onde `surveyId` estava sendo perdido
A causa raiz era o acoplamento ao contexto global: o form tinha apenas `data-survey-id`, não tinha `<input name="surveyId">`/`<input name="token">`, e `getPublicSurveyContext()` não aceitava `formEl`. Se `window.ValoraPublicSurveyState.context` ficasse vazio/stale, `buildPublicSurveySubmitPayload` gerava payload sem `surveyId`.

## 8. Onde `provider_unavailable` era gerado
O código `provider_unavailable` continua existindo em validadores/fluxos legados de provider, mas o fluxo de submit público agora preserva erros conhecidos via `normalizePublicSubmitError`. `invalid-argument` com mensagem `surveyId é obrigatório` é mapeado para `missing_survey_id`, e `token é obrigatório` para `missing_public_token`.

## 9. Como a correção garante que submit nunca acontece sem `surveyId`
- `renderTakeSurvey` não renderiza o form se o state não estiver `ready`, sem `surveyId`, sem `token`, sem `survey`, sem `form` ou com mismatch `survey.formId !== form.id`.
- O form renderizado contém campos hidden reais `surveyId`, `token` e `org`.
- `getPublicSurveyContext(formEl)` tem fallback em state, contexto legado, hidden inputs, dataset e query string.
- `buildPublicSurveySubmitPayload(formEl)` usa contexto + `data(formEl)` + cache.
- `validatePublicSubmitPayload` valida `payload.surveyId` e `payload.token` antes de qualquer dependência de `state.status`.
- `submitSurvey` possui guarda final imediatamente antes de `submitPublicSurveyResponse`.

## 10. Como testar resultado/e-mail depois
1. Abrir um link público real `?survey=<id>&token=<token>&org=<org>`.
2. Confirmar no DevTools que `[name="surveyId"]` e `[name="token"]` têm valor.
3. Enviar o formulário e observar o overlay `Aguarde, estamos processando sua requisição...`.
4. Confirmar no console que `[Valora] submitSurveyResponse payload` mostra `surveyId` preenchido.
5. A callable deve retornar `responseId`, `resultToken` e `resultEmail`; `handlePublicSubmitSuccess` redireciona para `?result=<responseId>&rt=<resultToken>` e `renderResult` exibe resultado, status do e-mail e CTA de WhatsApp.
