# Sprint Gateway Public Journey — Auditoria

## Diagnóstico
- A Home abre a pesquisa pública em `renderHome`, resolvendo a pesquisa com `resolveHomeFeaturedSurvey` e o link com `buildHomeFeaturedSurveyUrl`.
- A rota pública é decidida em `routeFromLocation`, que chama `renderTakeSurvey` quando encontra `?survey=` e `token=`.
- Antes da sprint, validação/submissão ainda admitiam branches Firebase/Cloud Functions em `renderTakeSurvey`, `submitSurvey` e `renderResult`.
- Agora a validação pública usa `validatePublicSurveyLink`, a submissão usa `submitPublicSurveyResponse` e resultado usa `loadPublicResult`.
- O resultado local é calculado em `calculateSurveyResult`; no gateway foi criado cálculo compatível em `communication-gateway/src/services/result-service.js`.
- O e-mail de resultado deve ser disparado pelo gateway após `POST /public/surveys/:surveyId/responses`, com registro em `communications`.
- Certificados PDF/PNG continuam no frontend via `buildCertificateViewModel`, `certificatePdf` e `exportCertificateImage`.

## Campos observados
- `survey`: `id`, `title`, `companyId`, `formId`, `status`, `expiresAt`, `token/publicToken/accessToken`, `lgpdRequired`, `requireIdentification`, `allowRepeat`.
- `response`: `id`, `surveyId`, `formId`, `companyId`, `participant`, `answers`, `createdAt/completedAt`, `resultToken/resultTokenHash`, scores, `emailStatus`.
- `result`: `rawScore`, `maxScore`, `totalScore`, `percentage`, `normalized5`, `maturityLabel`, `level`, `byDimension`, `dimensionScores`, `strongestDimension`, `weakestDimension`.

## Riscos
- Firestore precisa manter tokens públicos disponíveis ao Admin SDK do gateway sem vazar `tokenHash` ao frontend.
- SMTP pode falhar; a resposta deve permanecer salva e retornar `ok=true` com `emailStatus=failed`.
- Build IIS estático não pode chamar `/api/email/*` nem Cloud Functions quando `ENABLE_CLOUD_FUNCTIONS=false`.
- Resultado público deve depender de `responseId + resultToken`, nunca de dados calculados no navegador.
