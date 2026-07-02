# Auditoria — fluxo legado de submit público, erro 400 e loading

Data: 2026-07-02.

## Arquivos auditados

- `app.js`: renderização da jornada pública, montagem/envio do payload, loading, resultado, WhatsApp e status de e-mail.
- `firebase-repository.js`: chamada callable `submitSurveyResponse`, normalização de resposta e preservação de `details.code`.
- `repository.js`, `api-repository.js`, `config.js`: provedores/feature flags de dados e fallback de API.
- `functions/index.js`: contrato callable `submitSurveyResponse`, `getPublicResult` e `sendResultEmail`.
- `pdf.js`, `report-service.js`, `logger-service.js`, `log-service.js`: geração de relatório/logs sem necessidade de mudança direta no submit.
- `index.html`: boot do app legado por scripts estáticos.
- `scripts/build-production.js`: build/cópia de assets e cache busting.

## Mapa do fluxo

1. **Função que renderiza a pesquisa pública:** `renderTakeSurvey(sid, token, orgSlug, resolvedPayload)` em `app.js`.
2. **Função que monta o payload:** agora `buildPublicSurveySubmitPayload(form, context)` em `app.js`.
3. **Função que chama `ValoraRepository.submitPublicSurveyResponse`:** `submitPublicSurveyAuto(payload)` em `app.js`; ela chama `window.ValoraRepository.submitPublicSurveyResponse` quando disponível.
4. **Onde aparecia `provider_unavailable`:** `publicApiErrorCode` usava `provider_unavailable` como fallback, e `publicApiFriendlyError` tinha mensagem específica para esse código.
5. **Onde o erro original da Function era perdido:** `mapPublicFunctionError` em `firebase-repository.js` priorizava `err.code` e nem sempre preservava `err.details.code`, fazendo erros conhecidos caírem em mensagens genéricas.
6. **Onde deveria aparecer loading:** no submit do formulário `data-form="takeSurvey"`, antes da validação e durante `submitSurveyResponse`; o helper `withLoading` agora usa overlay global e spinner no botão.
7. **Campos obrigatórios enviados:** `surveyId`, `token`, `participant.name`, `participant.email`, `answers`, `lgpdConsent`, `communicationConsent`, além de `invitationId`, `participantId` e `department` quando presentes.
8. **Contrato atual esperado por `submitSurveyResponse`:** `{ payload: { surveyId, token, participant: { name, email, phone, company, department }, answers, lgpdConsent, communicationConsent, invitationId, participantId, department } }`.
9. **Como o resultado é aberto depois do submit:** `handlePublicSubmitSuccess` grava diagnóstico, monta `?result=<responseId>&rt=<resultToken>` e chama `renderResult(responseId, true, resultToken)`.
10. **Como o e-mail de resultado é tratado:** a Function retorna `resultEmail.status`; `getPublicResult` também retorna esse status. A tela de resultado exibe status `queued`/`failed_non_blocking` e oferece reenvio via `ValoraRepository.sendResultEmail(responseId, { resultToken })`.

## Plano de corte .NET/PostgreSQL

- O contrato de UI foi isolado no payload normalizado, permitindo alternar o provider por feature flag futura `PUBLIC_SUBMISSION_PROVIDER=cloud-functions|dotnet-api` sem alterar a tela.
- O endpoint esperado é `POST /public/surveys/{surveyId}/responses` e deve retornar `{ responseId, resultToken, score, level, message, resultEmail }`.
- A persistência PostgreSQL deve cobrir `responses`, `response_answers`, `result_scores`, `dimension_scores`, `email_jobs` e `audit_logs`.
