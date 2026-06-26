# DATABASE_SCHEMA_VALORA

Atualizado na Sprint 19 para manter produção em Firebase por padrão e permitir API/PostgreSQL apenas em ambiente local/controlado.

## Pontos principais
- Produção permanece com `DATA_PROVIDER='firebase'` e `ALLOW_API_PRODUCTION_CUTOVER=false`.
- Jornada pública real usa `POST /public/surveys/{surveyId}/validate`, `POST /public/surveys/{surveyId}/responses` e `POST /public/results/{responseId}`.
- Submissão pública grava dados transacionais em `valorapesquisa.responses`, `response_answers`, `result_scores`, `dimension_scores`, `certificates`, `email_jobs`, `communications` e `audit_logs`.
- `resultToken` é retornado uma única vez no submit; o banco armazena somente `result_token_hash`.
- Frontend continua Bootstrap + JavaScript puro, sem secrets e sem remover Firebase.
- Docker usa `docker compose`; Windows fora do Docker usa `dotnet build backend\Valora.sln` e os validadores em `tools/windows/59-validar-jornada-publica-real-api-postgres.bat`.
