# Auditoria da jornada pública .NET + PostgreSQL

## Legado auditado
O frontend legado (`index.html`, `app.js`, repositórios JS, `pdf.js`, serviços de relatório/notificação/analytics) e `functions/index.js` permanecem como referência de paridade para validação de link, submissão, resultado público, e-mail, certificado e pesquisa em destaque. O fluxo oficial corrigido não depende deles para CRUD.

## Backend .NET auditado e atualizado
- `PublicSurveysController`: mantém `POST /public/surveys/{surveyId}/validate` e `POST /public/surveys/{surveyId}/responses`.
- `PublicResultsController`: agora expõe `GET/POST /public/results/{responseId}`, certificado HTML, PDF, PNG e reenvio público de e-mail.
- `PublicSurveyService`, `PublicSurveySubmitter` e `PublicResponseTransactionService`: persistem resposta, respostas normalizadas, pontuação, dimensões, certificado, email job e auditoria em transação PostgreSQL.
- `CertificateService`: renderiza HTML, PDF e PNG binários reais sem expor hashes.
- Repositories Dapper existentes (`ResponseRepository`, `ResultRepository`, `CertificateRepository`, `CommunicationRepository`) foram usados como fonte oficial PostgreSQL.

## Endpoints criados/corrigidos
- `GET /public/results/{responseId}?token=<resultToken>`
- `GET /public/results/{responseId}/certificate?token=<resultToken>`
- `GET /public/results/{responseId}/certificate.pdf?token=<resultToken>`
- `GET /public/results/{responseId}/certificate.png?token=<resultToken>`
- `POST /public/results/{responseId}/email?token=<resultToken>`

## Telas atualizadas
- `PublicSurvey/Take.cshtml` + `public-survey-page.js`: submissão API-first com DTO oficial, loading e redirecionamento para resultado com token.
- `Results/Public.cshtml` + `result-page.js`: resultado, dimensões, certificado PDF/PNG, reenvio e CTA WhatsApp.
- `loading.js`: `setGlobalLoading`, `withLoading` e `setButtonLoading`.

## Banco e scripts
O script oficial continua em `database/postgresql/scriptbd_completo.sql` e os validadores conferem idempotência, tabelas e índices essenciais.

## Funcionalidades migradas
Resposta pública, cálculo do resultado, criação de certificado, enfileiramento/reenvio de e-mail, página de resultado, downloads PDF/PNG, WhatsApp CTA e loading visual agora são validados no caminho .NET + Dapper + PostgreSQL.
