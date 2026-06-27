# Sprint 23 — Auditoria de Logging, Erros e Observabilidade

## Diagnóstico objetivo
1. Sim, `ErrorHandlingMiddleware` captura exceções não tratadas.
2. Sim, retorna JSON `{ ok, message, code, traceId, correlationId }`.
3. Sim, `CorrelationIdMiddleware` lê/gera `X-Correlation-Id`.
4. Sim, Serilog usa `Enrich.FromLogContext()`.
5. Sim, serviços críticos principais recebem `ILogger<T>`.
6. Sim, `PublicResponseTransactionService` loga início, inserts, commit, rollback e erro.
7. Sim, `PublicSurveySubmitter` loga sem token público/respostas abertas.
8. Sim, `PublicResultService` loga consulta sem registrar o token.
9. Sim, `MigrationRunner` loga scan, aplicado, já aplicado e falha.
10. Parcial: repositories permanecem com Dapper direto; próximos incrementos devem ampliar contexto por método.
11. Sim, envio/planejamento de e-mail possui try/catch técnico e status `failed`/`last_error`.
12. Sim, `LogSanitizer` mascara e-mail, telefone, documento, token e connection string.
13. Sim, existe `LogSanitizer`.
14. Sim, middleware mapeia exceções para HTTP.
15. Sim, `ValidationAppException`.
16. Sim, `NotFoundAppException`.
17. Sim, `BusinessRuleAppException`.
18. Sim, `ForbiddenAppException`; `UnauthorizedAccessException` cobre não autorizado.
19. Sim, testes criados para erro global.
20. Parcial: validadores cobrem padrão de rollback; teste unitário profundo depende de fakes transacionais.
21. Sim, política e validadores impedem vazamento no JSON/log.
22. Sim, `validate-no-sensitive-logs.js` impede regressão em logs técnicos.
23. Sim, `api-client.js` propaga mensagem amigável e correlationId.

## Separação de auditoria e log técnico
- Log técnico: Serilog/ILogger para operação, falhas, rollback, migration e e-mail.
- Auditoria de negócio: `audit_logs` apenas para eventos de negócio como submissão pública e autenticação.

## Riscos restantes
- Ampliar try/catch contextual em todos os repositories críticos sem reduzir legibilidade.
- Rodar `dotnet build/test` em ambiente com SDK .NET disponível.
