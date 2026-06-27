
## Sprint 23 — Observabilidade e Tratamento de Erros
- Erros HTTP devem ser tratados pelo middleware global e retornar JSON padronizado com `ok=false`, `code`, `traceId` e `correlationId`.
- Toda request recebe `X-Correlation-Id`; o valor entra no Serilog `LogContext`.
- Logs técnicos usam `ILogger<T>` com propriedades estruturadas e não substituem auditoria de negócio.
- Dados sensíveis devem ser mascarados por `LogSanitizer`; não registrar senha, token puro, hash de token, CPF, telefone completo, e-mail completo, secret de Firebase, SMTP password ou connection string completa.
- Transações devem logar início, sucesso, commit, rollback, falha de rollback e relançar exceções.
- Falhas de e-mail devem virar status operacional (`failed`, `failed-config`, `pending-provider`) sem vazar segredo.
