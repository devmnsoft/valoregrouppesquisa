# Contrato API usado pelo Valora.Web

- POST `/auth/login`
- POST `/auth/register-company`
- POST `/auth/forgot-password`
- POST `/auth/reset-password`
- GET `/me`
- GET `/plans/public`
- POST `/public/surveys/{surveyId}/validate`
- POST `/public/surveys/{surveyId}/responses`
- POST `/public/results/{responseId}`
- GET `/certificates/{certificateCode}/validate`
- GET `/responses/{responseId}/certificate.pdf`
- GET `/responses/{responseId}/certificate.png`
- GET `/health`, `/health/database`, `/health/logging`, `/health/migration`, `/health/version`
