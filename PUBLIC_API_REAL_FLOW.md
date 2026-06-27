# PUBLIC_API_REAL_FLOW

Atualizado na Sprint 19 para manter produção em Firebase por padrão e permitir API/PostgreSQL apenas em ambiente local/controlado.

## Pontos principais
- Produção permanece com `DATA_PROVIDER='firebase'` e `ALLOW_API_PRODUCTION_CUTOVER=false`.
- Jornada pública real usa `POST /public/surveys/{surveyId}/validate`, `POST /public/surveys/{surveyId}/responses` e `POST /public/results/{responseId}`.
- Submissão pública grava dados transacionais em `valorapesquisa.responses`, `response_answers`, `result_scores`, `dimension_scores`, `certificates`, `email_jobs`, `communications` e `audit_logs`.
- `resultToken` é retornado uma única vez no submit; o banco armazena somente `result_token_hash`.
- Frontend continua Bootstrap + JavaScript puro, sem secrets e sem remover Firebase.
- Docker usa `docker compose`; Windows fora do Docker usa `dotnet build backend\Valora.sln` e os validadores em `tools/windows/59-validar-jornada-publica-real-api-postgres.bat`.


## Sprint 21

Jornada pública refatorada com read models tipados, services pequenos, resultToken com hash e submissão transacional em PostgreSQL para ambiente local/controlado. Produção permanece Firebase.

## Sprint 23 — Observabilidade e Tratamento de Erros
- Erros HTTP devem ser tratados pelo middleware global e retornar JSON padronizado com `ok=false`, `code`, `traceId` e `correlationId`.
- Toda request recebe `X-Correlation-Id`; o valor entra no Serilog `LogContext`.
- Logs técnicos usam `ILogger<T>` com propriedades estruturadas e não substituem auditoria de negócio.
- Dados sensíveis devem ser mascarados por `LogSanitizer`; não registrar senha, token puro, hash de token, CPF, telefone completo, e-mail completo, secret de Firebase, SMTP password ou connection string completa.
- Transações devem logar início, sucesso, commit, rollback, falha de rollback e relançar exceções.
- Falhas de e-mail devem virar status operacional (`failed`, `failed-config`, `pending-provider`) sem vazar segredo.
