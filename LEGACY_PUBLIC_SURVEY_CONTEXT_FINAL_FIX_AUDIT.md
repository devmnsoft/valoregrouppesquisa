# Auditoria final — contexto da pesquisa pública legado

## 1. Leitura de `?survey=`, `?token=` e `?org=`
A rota pública passa a ler os parâmetros em uma única fonte, `getPublicSurveyRouteParams()`, aceitando `survey`/`surveyId`, `token` e `org`. O helper `hasPublicSurveyRouteParams()` bloqueia links incompletos e `isDemoOrInvalidPublicRoute()` bloqueia links demo/hash em produção.

## 2. Chamada da home para `getFeaturedHomeSurvey`
A home continua usando `getFeaturedHomeSurvey()`/`getFeaturedHomeSurveyUrl()` para obter o diagnóstico gratuito em destaque. O botão de erro agora usa `openOfficialFreeSurveyFromError()` para buscar uma URL pública oficial nova antes de redirecionar.

## 3. Chamada de `validateSurveyLink`
A validação remota é centralizada por `resolvePublicSurveyContext()`, que chama `validatePublicSurveyLink({ surveyId, token, org })` somente depois de validar rota não vazia e não demo.

## 4. Renderização do formulário público
`renderTakeSurvey()` agora renderiza o formulário apenas após `ValoraPublicSurveyState.status === 'ready'` e presença de `context.surveyId`, `context.token`, `context.survey` e `context.form`.

## 5. Registro do submit
O submit permanece pelo formulário `data-form="takeSurvey"`, mas o caminho Firebase valida o estado público e o payload antes de chamar `submitPublicSurveyResponse()`.

## 6. Montagem do payload
`buildPublicSurveySubmitPayload()` usa somente `getPublicSurveyContext()`/state validado para obter `surveyId` e `token`; não usa mais dataset ou hidden input como fonte de verdade.

## 7. Onde `surveyId` estava sendo perdido
O fluxo anterior permitia renderizar formulário mesmo quando a home/diagnóstico público estava indisponível. O payload podia cair para DOM/dataset/hidden input vazio, gerando chamada à Function sem `surveyId`.

## 8. Formulário com diagnóstico indisponível
A correção introduz `renderPublicSurveyUnavailable()` como retorno exclusivo para contexto inválido. Essa tela mostra CTA de recuperação, WhatsApp e retry, sem perguntas ou formulário abaixo.

## 9. `provider_unavailable` mascarando erro real
`normalizePublicSubmitError()` preserva códigos de validação como `missing_survey_id` e `missing_public_token`; a renderização de erro usa o código normalizado em vez de substituí-lo por indisponibilidade genérica.

## 10. Correções feitas
- State machine global `window.ValoraPublicSurveyState`.
- Resolução centralizada do contexto público.
- Bloqueio de render sem contexto validado.
- Payload vindo de contexto validado.
- Validação pré-submit antes de chamar Function.
- Loading global e de botão no submit.
- Retry seguro sem submit.
- Botão para gerar link oficial novo.
- Function `submitSurveyResponse` já compatível com payload legado/aninhado e erros claros.
- Validadores estáticos para impedir regressão.
