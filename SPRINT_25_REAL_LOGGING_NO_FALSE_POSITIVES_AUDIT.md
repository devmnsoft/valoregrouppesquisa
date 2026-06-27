# Sprint 25 — Auditoria de logging real sem falso positivo

## Diagnóstico objetivo
1. `validate-repository-logging.js` aceitava `ILogger<T>`, `catch`, `LogError(ex` e `throw;` em comentários; foi refeito para remover comentários antes da análise.
2. Comentários simulando implementação foram encontrados e removidos em `SurveyRepository`, `FormRepository`, `ResponseRepository`, `ResultRepository`, `CertificateRepository`, `MigrationRepository`, `UserRepository`, `PlanRepository` e `OrganizationRepository`.
3. Os repositories críticos sem `ILogger<T>` real eram os mesmos listados acima; todos receberam `ILogger<T>` no construtor.
4. Os métodos Dapper críticos desses repositories não tinham `try/catch` real; agora logam erro inesperado e relançam.
5. Os repositories corrigidos agora usam `logger.LogError(ex, ...)` real.
6. Os catchs críticos usam `throw;` para preservar stack trace.
7. Services críticos já tinham logger nos pontos operacionais principais; a homologação completa deve manter cobertura em todos os serviços listados.
8. Rollback transacional público já logava falha de rollback e erro raiz.
9. Não foram mantidos catchs silenciosos nos repositories corrigidos.
10. Contextos sensíveis foram mascarados quando e-mail, telefone, documento ou token aparecem nos parâmetros.
11. Validadores que não removiam comentários: `validate-repository-logging.js`; helper compartilhado criado para uso gradual pelos demais.
12. Scripts Node com risco de falso positivo por comentário: validadores baseados em `includes`; o validador anti-falso-positivo passa a bloquear comentários simulados.
13. Garantia automatizada criada: `scripts/validate-no-validator-fake-comments.js`.
14. Para homologação operacional restam executar a bateria completa em ambiente Windows/IIS com Docker, PostgreSQL e .NET SDK disponíveis.
