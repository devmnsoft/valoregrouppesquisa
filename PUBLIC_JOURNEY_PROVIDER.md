# Provider da Jornada Pública

## Funções únicas

A jornada pública deve usar funções centralizadas:

- `validatePublicSurveyLink(payload)`
- `submitPublicSurveyResponse(payload)`
- `loadPublicResult(responseId, resultToken)`

## Seleção por `DATA_PROVIDER`

- `firebase`: mantém fluxo atual com Firestore/local e Firebase Functions apenas se explicitamente habilitadas.
- `api`: usa `ValoraApiRepository` e endpoints ASP.NET Core.
- `hybrid`: chama API e Firebase/local quando possível, priorizando segurança e comparação controlada.

## Regra Cloud Functions

Se `ENABLE_CLOUD_FUNCTIONS=false`, a jornada pública não deve chamar `callPublicFunction`. O validador `scripts/validate-public-journey-provider.js` falha se encontrar chamada direta dentro de `renderTakeSurvey` ou `submitSurvey`.

## Endpoints API preparados

- `POST /public/surveys/{surveyId}/validate`
- `POST /public/surveys/{surveyId}/responses`
- `POST /public/results/{responseId}`
