# Sprint 26 — Auditoria de logging real em repositories e validadores

## Escopo auditado

Foram auditados os diretórios e arquivos solicitados: `backend/Valora.Infrastructure/Repositories/`, `backend/Valora.Application/Services/`, `backend/Valora.Infrastructure/Database/`, `backend/Valora.Infrastructure/Email/`, `backend/Valora.Api/Middleware/`, `backend/Valora.Api/Controllers/`, `scripts/`, `migration/` e `package.json`.

## Diagnóstico objetivo

1. **Repositories com `ILogger<T>` apenas em comentário:** nenhum encontrado após a correção de Sprint 26. O validador anti-falso-positivo falha se comentários em código contiverem `ILogger<`.
2. **Repositories sem `ILogger<T>` real no construtor:** nenhum dos repositories críticos. `SurveyRepository`, `FormRepository`, `ResponseRepository`, `ResultRepository`, `CertificateRepository`, `CommunicationRepository`, `AuditRepository`, `MigrationRepository`, `UserRepository`, `PlanRepository` e `OrganizationRepository` recebem `ILogger<T>` real.
3. **Métodos Dapper críticos sem `try/catch`:** nenhum dos métodos críticos listados na sprint ficou sem bloco `try/catch` real nos repositories críticos.
4. **`catchs` sem `logger.LogError(ex, ...)`:** nenhum catch crítico identificado nos repositories críticos. Os validadores removem comentários antes de procurar `LogError(ex, ...)`.
5. **`catchs` sem `throw;`:** nenhum catch crítico identificado nos repositories críticos. Os métodos críticos relançam exceções inesperadas com `throw;`.
6. **`catchs` que engolem exceção ou retornam `null` indevidamente:** nenhum identificado nos repositories críticos. Retornos nulos permanecem apenas como resultado natural de `QuerySingleOrDefaultAsync`, fora de `catch`.
7. **Validadores que ainda analisam comentário como código:** os validadores operacionais da sprint usam `scripts/lib/strip-comments.js` nos pontos reforçados. O validador anti-falso-positivo analisa comentários separadamente para bloquear contratos falsos.
8. **Scripts Node sem `try/catch` no `main`:** os scripts de migração principais possuem `main` com tratamento de erro e saída não-zero em falha. `scripts/validate-real-migration-dry-run.js` foi considerado script validador operacional e coberto por validação de logging de migração.
9. **Scripts de migration sem log de início, etapa, erro e duração:** os scripts principais usam `migration/migration-logger.js` para início, etapa, sucesso, erro sanitizado e `durationMs`.
10. **Logs com risco de vazamento sensível:** riscos mitigados nos pontos críticos por `LogSanitizer.MaskEmail`, `MaskPhone`, `MaskDocument`, `MaskToken` e validação `api:no-sensitive-logs`. Atenção contínua: não adicionar dados completos em mensagens de log futuras.
11. **Services críticos sem `ILogger<T>`:** os services críticos revisados possuem logger real, incluindo submissão pública, resultado público, autenticação, planos, certificados, email e SMTP.
12. **Integrações externas sem timeout/log adequado:** SMTP possui logging sanitizado de falha. Integrações de health e migração registram falhas com contexto seguro. Recomenda-se na próxima sprint padronizar timeouts explícitos por configuração para todo cliente externo.
13. **Health checks sem cobertura database/logging/migration/version:** endpoints `/health`, `/health/database`, `/health/logging`, `/health/migration` e `/health/version` existem e são validados por `api:health-observability`.
14. **Frontend exibe `correlationId` em erro de API:** `api-client.js`, `api-repository.js` e `app.js` normalizam erro, preservam `message`, `code`, `traceId`/`correlationId` e exibem código de atendimento sem stack trace.
15. **Pendências para homologação operacional:** executar a bateria completa em ambiente Windows/IIS com PostgreSQL e Docker disponíveis, incluindo build Docker, health PRD e `npm run prod:health` contra o domínio produtivo.

## Mapeamento de termos sensíveis e operacionais

- `ILogger<`: permitido apenas em código real C# e documentação; bloqueado em comentários de código validável.
- `catch (Exception ex)`: permitido apenas em código real; bloqueado em comentários de código validável.
- `logger.LogError` / `LogError(ex`: permitido apenas em código real; bloqueado em comentários de código validável.
- `throw;`: permitido apenas em código real; bloqueado em comentários de código validável.
- `Console.WriteLine`, `console.log`, `console.error`: uso deve ser restrito a scripts operacionais/validadores e sem segredos.
- `password`, `senha`, `token`, `resultToken`, `result_token_hash`, `publicToken`, `token_hash`, `cpf`, `document`, `telefone`, `phone`, `email`, `connectionString`, `private_key`, `smtp`: termos mapeados como sensíveis. Logs devem usar mascaramento, status booleano, IDs técnicos ou metadados não sensíveis.

## Falsos positivos encontrados e removidos

- O contrato falso de logging por comentário foi tratado como risco real da base.
- O validador `validate-no-validator-fake-comments.js` agora bloqueia comentários com `ILogger<`, `catch (Exception ex)`, `logger.LogError`, `LogError(ex`, `throw;`, `validate:`, `PASS`, `Sprint 24 operational logging contract` e `Sprint 25 operational logging contract`.

## Resultado dos validadores locais

- `api:no-fake-validator-comments`: passou.
- `api:repository-logging`: passou.
- `api:no-sensitive-logs`: passou.
- `api:error-handling`: passou.
- `api:correlation`: passou.
- `api:transaction-logging`: passou.
- `api:email-errors`: passou.
- `api:health-observability`: passou.
- `frontend:api-errors`: passou.

## Riscos restantes

- A garantia de não vazamento depende de manter a disciplina em novos logs e de revisar PRs futuros com os validadores.
- A homologação Windows/IIS, Docker e PRD depende de infraestrutura externa disponível no ambiente de execução.
- Próxima sprint recomendada: padronizar timeouts/retries configuráveis para integrações externas, adicionar testes de contrato para todos os validadores e publicar runbook de resposta a incidentes com correlationId.
