# Auditoria — correção definitiva do submit público legado sem `surveyId`

## Arquivos auditados

- `app.js`
- `firebase-repository.js`
- `repository.js`
- `config.js`
- `functions/index.js`
- `style.css`
- `scripts/build-production.js`

## 1. Onde a URL pública é lida

A URL pública é lida no boot/roteamento por `routeParamsFromLocation()` em `app.js`, que extrai `survey`, `token`, `org`, `result`, `rt` e `certificate`. Também foi criado `getPublicSurveyRouteParams()` como fonte única para o fluxo público, lendo `survey`, `surveyId`, `token` e `org` diretamente de `location.search`.

## 2. Onde `surveyId`/`token`/`org` são extraídos

`publicSurveyRouteContract()` normaliza os parâmetros para `surveyId`, `token` e `org`. O novo `getPublicSurveyRouteParams()` faz a extração resiliente (`survey` ou `surveyId`) e `getPublicSurveyContext()` une contexto validado com a URL atual.

## 3. Onde `validateSurveyLink` salva contexto

O fluxo `renderTakeSurvey()` chama `validatePublicSurveyLink({ surveyId, token, org })`, que delega para `ValoraRepository.validatePublicSurvey()`/Function `validateSurveyLink`. Após validação bem-sucedida, `renderTakeSurvey()` agora chama `setPublicSurveyContext({ surveyId, token, org, survey, form, company })` antes de renderizar o formulário.

## 4. Onde `renderTakeSurvey` renderiza formulário

`renderTakeSurvey()` renderiza o formulário público com `data-form="takeSurvey"`. O formulário agora também recebe `data-public-survey-form`, `data-survey-id`, `data-token-present="true"` e `data-org`, preservando `surveyId` em atributo seguro sem expor token puro em `data-*`.

## 5. Onde o payload do submit é montado

O payload é montado em `buildPublicSurveySubmitPayload(formEl)`. A função agora usa `getPublicSurveyContext()` como origem principal, cai para `formEl.dataset.surveyId` se necessário, coleta participante, respostas, consentimentos e grava `window.ValoraRuntimeDiagnostics.lastSubmitPayload` com dados sanitizados.

## 6. Onde `submitPublicSurveyResponse` é chamado

O submit público é tratado por `submitSurvey(form)`, no branch Firebase. O fluxo agora monta payload, valida com `validatePublicSubmitPayload()`, registra diagnóstico e só então chama `submitPublicSurveyResponse(submitPayload)`/`ValoraRepository.submitPublicSurveyResponse(payload)`.

## 7. Onde `provider_unavailable` era gerado

O mascaramento acontecia na camada de erro público quando erros da Function/400 eram convertidos para mensagens genéricas do provedor. A correção adiciona `normalizePublicSubmitError(err)` em `app.js` e mapeia `missing_survey_id`/`missing_public_token` em `firebase-repository.js`, preservando códigos reais em vez de renderizar `provider_unavailable` para erros conhecidos.

## 8. Por que `surveyId` estava chegando vazio

A causa raiz era a dependência do formulário/hidden inputs e do cache `publicSurveyCache` para manter `surveyId`/`token`. Em erro, retry, rerender ou links incompletos/demo, o contexto podia ser perdido ou reconstruído sem `surveyId`, permitindo que o submit chamasse a Function com payload incompleto. Além disso, a Function aceitava apenas o contrato aninhado esperado e não normalizava formatos legados/duplo-aninhados.

## 9. Correções aplicadas

- Criado contexto público único (`getPublicSurveyRouteParams`, `setPublicSurveyContext`, `getPublicSurveyContext`).
- Contexto salvo imediatamente após validação do link público.
- Formulário renderizado com `data-public-survey-form` e `data-survey-id`.
- `buildPublicSurveySubmitPayload()` refeito para preservar `surveyId`/`token` e diagnosticar payload sanitizado.
- `validatePublicSubmitPayload()` bloqueia localmente `missing_survey_id`, `missing_public_token`, participante, e-mail, LGPD e perguntas obrigatórias antes da Function.
- Submit público mantém loading global com “Aguarde, estamos processando sua requisição...”, bloqueia duplo clique e preserva respostas em erro local.
- Erros de submit normalizados para mostrar código real, especialmente `missing_survey_id`.
- Retry seguro: só reabre formulário se houver `surveyId` e `token`; sem contexto, orienta voltar ao diagnóstico gratuito.
- `submitSurveyResponse` em `functions/index.js` aceita payload flat, `payload` aninhado e `payload.payload`, além de aliases `survey`, `publicToken` e `accessToken`.
- Criados validadores de regressão para contexto, bloqueio de submit sem `surveyId`, compatibilidade da Function, loading, erros, botões e redirecionamento de sucesso.

## 10. Como testar

### Automatizado

```bash
npm run check
npm run legacy:public-context
npm run legacy:submit-never-without-surveyid
npm run functions:submit-backward-compatible
npm run legacy:submit-no-provider-mask
npm run legacy:submit-loading-visible
npm run legacy:submit-error-buttons-safe
npm run legacy:submit-success-result
npm run build:prod
```

### Manual

1. Abrir `?survey=<id>&token=<token>&org=<org>` e conferir `window.ValoraRuntimeDiagnostics.lastPublicSurveyContext`.
2. Enviar sem nome/e-mail e confirmar erro local `participant_required` sem chamada à Function.
3. Enviar com LGPD desmarcado e confirmar erro local `lgpd_required` sem chamada à Function.
4. Enviar válido e confirmar loading, payload com `surveyId`, retorno `responseId/resultToken` e abertura do resultado.
5. Forçar payload sem `surveyId` e confirmar `missing_survey_id`, nunca `provider_unavailable`.
6. Clicar “Tentar novamente” sem contexto e confirmar que não há submit; aparece orientação para voltar ao diagnóstico gratuito.
