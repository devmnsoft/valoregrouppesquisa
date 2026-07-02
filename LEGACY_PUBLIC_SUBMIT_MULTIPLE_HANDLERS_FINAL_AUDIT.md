# Auditoria final — submit público legado com múltiplos handlers

## Escopo auditado
Arquivos auditados: `app.js`, `firebase-repository.js`, `repository.js`, `api-repository.js`, `functions/index.js`, `style.css` e `scripts/validate-*.js`.

## 1. Lugares que chamam `submitSurveyResponse`
- `app.js`: chamada callable direta existe apenas dentro de `submitPublicSurveyViaCloudFunction(payload)`, após `assertPublicSubmitPayloadReady(payload)`.
- `firebase-repository.js`: `submitPublicSurveyResponseFirebase(payload)` chama `callFunction('submitSurveyResponse', { payload })`, após `assertPublicSubmitPayloadReady(payload)`.
- `functions/index.js`: exporta `exports.submitSurveyResponse=onCall` e usa `publicSubmitPayloadFromRequest(req)` com erro `missing_survey_id` em `details.code` quando o identificador não vem no payload.
- Validadores em `scripts/validate-*.js` citam o nome apenas para garantir o contrato e bloquear regressões.

## 2. Lugares que chamam `submitPublicSurveyResponse`
- `app.js`: somente `guardedPublicSurveySubmit(formEl, event)` chama `submitPublicSurveyResponse(payload)` no caminho público oficial, depois de validação de payload/contexto.
- `app.js`: providers auxiliares (`submitPublicSurveyAuto`, `submitPublicSurveyViaExternalApi`, `submitPublicSurveyViaApi`) mencionam `submitPublicSurveyResponse` apenas como camada interna/provider, todos com `assertPublicSubmitPayloadReady(payload)`.
- `repository.js`, `api-repository.js` e `firebase-repository.js`: expõem providers, agora protegidos por guard de payload antes de qualquer rede.

## 3. Event listeners de submit
- Há um único listener global de submit em `registerGlobalHandlers()`: `document.addEventListener('submit', handleDocumentSubmit)`.
- `handleDocumentSubmit(e)` roteia por `formHandlers[formName]` e passa `(form, e)`.
- Não há listener direto paralelo de submit público chamando a Function.

## 4. `data-form="takeSurvey"`
- O formulário público renderizado por `renderTakeSurvey` possui `data-form="takeSurvey"` e `data-public-survey-form`.
- O handler `takeSurvey` em `createFormHandlers()` encaminha para `submitSurvey(form, e)`, que delega imediatamente para `guardedPublicSurveySubmit(form, event)` quando o form é público.

## 5. Handlers mapeados em `createFormHandlers/formHandlers`
- `formHandlers` é inicializado uma vez no bootstrap via `formHandlers=createFormHandlers()`.
- O mapeamento oficial para pesquisa pública é `takeSurvey:(form,e)=>submitSurvey(form,e)`.
- `submitSurvey(form, event)` é compatível com legado, mas para `data-public-survey-form`/`data-form="takeSurvey"` só chama `guardedPublicSurveySubmit`.

## 6. Handlers `data-action` relacionados a submit/pesquisa
- `retryPublicSurvey` apenas recarrega o link atual via `renderTakeSurvey(...)` ou abre o diagnóstico gratuito oficial; não chama submit.
- Ações de pesquisa/admin (`goSurveys`, `debugFeaturedHomeSurvey`, `repairFreeSurveyPublicLink`, etc.) não fazem POST de resposta pública.
- Botões de erro usam `openOfficialFreeSurveyFromError`, WhatsApp ou `retryPublicSurvey`, sem submit.

## 7. Caminho que chamava a Function após `missing_survey_id`
- O caminho legado era o submit de `takeSurvey`/`submitSurvey(form)` com chamada direta a `submitPublicSurveyResponse(submitPayload)` mesmo após outro fluxo já exibir `missing_survey_id`.
- Como o event delegation não parava todos os caminhos antigos, ainda era possível chegar ao provider callable.

## 8. Caminho mantido como único oficial
- Caminho oficial único: `handleDocumentSubmit` → `formHandlers.takeSurvey(form,e)` → `submitSurvey(form,e)` → `guardedPublicSurveySubmit(formEl,event)` → `validatePublicSubmitPayload(...)` + checagem `surveyId/token` → `submitPublicSurveyResponse(payload)` → providers guardados.

## 9. Caminhos removidos/bloqueados
- `submitSurvey(form)` público foi reduzido a delegador para `guardedPublicSurveySubmit`.
- Providers (`submitPublicSurveyResponse`, `submitPublicSurveyAuto`, `submitPublicSurveyViaCloudFunction`, `submitPublicSurveyViaFunctions`, `submitPublicSurveyViaExternalApi`, `submitPublicSurveyViaApi`, `submitPublicSurveyResponseFirebase`) bloqueiam payload sem `surveyId`/`token` via `assertPublicSubmitPayloadReady`.
- `retryPublicSurvey` foi bloqueado contra qualquer chamada de submit.
- `renderPublicSurveyUnavailable` limpa artefatos de formulário e substitui o `#app` inteiro.

## 10. Evidência de chamada única
- Validador `scripts/validate-legacy-single-public-submit-path.js` falha se houver `callFirebaseFunction('submitSurveyResponse')` fora de `submitPublicSurveyViaCloudFunction`.
- Validador `scripts/validate-legacy-submit-provider-guard.js` falha se qualquer provider público não chamar `assertPublicSubmitPayloadReady(payload)`.
- Validador `scripts/validate-legacy-submit-never-without-surveyid.js` falha se a validação local não ocorrer antes da chamada oficial.
- Log de debug agora imprime JSON com `surveyId`, `hasToken`, `org`, `answersCount`, `hasName`, `hasEmail` e `lgpdConsent`, além de gravar `window.ValoraRuntimeDiagnostics.lastPublicSubmitDebug`.
