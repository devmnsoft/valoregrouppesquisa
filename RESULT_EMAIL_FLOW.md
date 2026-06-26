# Sprint de Integração Real da Jornada Pública com Gateway

- Produção Spark usa `PUBLIC_SURVEY_VALIDATION_PROVIDER`, `PUBLIC_SUBMISSION_PROVIDER` e `RESULT_PROVIDER` como `external-api`.
- O frontend não contém segredos SMTP, tokens WhatsApp ou credenciais do gateway.
- A pesquisa destaque é aberta pela Home e enviada para `/public/surveys/:surveyId/responses`.
- O gateway valida token/status/expiração, calcula resultado, salva resposta e registra comunicação.
- Falha de e-mail não quebra a submissão; `emailStatus` retorna `failed`/`pending`/`sent`.
- Certificados PDF/PNG continuam gerados no frontend a partir do resultado seguro.
- QA obrigatório: `npm run check`, validadores em `scripts/validate-*.js`, build produção e healthcheck PRD.
