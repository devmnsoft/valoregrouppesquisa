# Provider da Jornada Pública

As funções oficiais são `validatePublicSurveyLink`, `submitPublicSurveyResponse` e `loadPublicResult`. Elas respeitam `PUBLIC_SURVEY_VALIDATION_PROVIDER`, `PUBLIC_SUBMISSION_PROVIDER`, `RESULT_PROVIDER` e só usam Cloud Functions quando `ENABLE_CLOUD_FUNCTIONS === true`.
