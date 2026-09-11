# FUNCTIONS_PUBLIC_SURVEY_SUBMIT

Documento operacional da Sprint 69.

- Pesquisa gratuita oficial: a submissão usa exclusivamente a Cloud Function `submitSurveyResponse`. Não há fallback de escrita direta no Firestore nem para API externa; uma falha da Function é exibida como falha de submissão, sem troca silenciosa de provedor.
- provider_unavailable só pode ocorrer após todos os providers falharem ou retornarem resultado inválido.
- Diagnósticos runtime ficam em window.ValoraRuntimeDiagnostics.lastPublicSubmit com mensagens sanitizadas.
- E-mail pós-submit é best-effort e não bloqueia a renderização do resultado/certificado.
- Deploy: npm run functions:deploy e npm run hosting:deploy após os validadores obrigatórios.
