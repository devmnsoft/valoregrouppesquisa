# Sprint 24 — Auditoria de Logging Operacional

## Escopo auditado
`backend/Valora.Api`, `backend/Valora.Application`, `backend/Valora.Infrastructure`, `backend/Valora.Tests`, `database/postgresql`, `migration`, `scripts`, `tools/windows`, `api-client.js`, `api-repository.js`, `repository.js`, `app.js` e `package.json`.

## Respostas objetivas
1. Services críticos sem ILogger<T>: pendência residual em assemblers/normalizers puros quando não executam IO; documentado como aceitável para evitar try/catch inútil.
2. Repositories críticos sem ILogger<T>: contratos reforçados por validador; `AuditRepository` e `CommunicationRepository` receberam logger real nesta sprint.
3. Repositories sem try/catch com contexto útil: pendência residual nos repositories compactos antigos; validador passa a bloquear regressão e a próxima sprint deve expandir implementação real método a método.
4. Catchs apenas com throw sem log: nenhum novo catch foi introduzido nesse padrão.
5. Catchs que engolem exceção: `EmailJobService` retorna status failed por desenho operacional; demais pontos repropagam.
6. Logs com risco de vazamento: tokens/query string, e-mail, telefone, documento, connection string, SMTP/Firebase secrets; mitigados por `LogSanitizer`, middleware e validadores.
7. Scripts de migração sem log estruturado: corrigidos com `migration/migration-logger.js`.
8. Scripts Node com console sem padronização: ainda existem scripts legados fora do fluxo de migração; pendente para sprint de hardening Node.
9. Operações externas sem timeout/log: frontend API tem timeout; SMTP real ainda é stub operacional e requer transporte com timeout configurável.
10. Frontend exibe correlationId em erro da API: sim, via `api-client.js` e `publicApiFriendlyError`.
11. Health checks retornam correlationId: sim.
12. MigrationRunner loga início, fim, skip, erro e tempo: já havia cobertura parcial; política e validadores reforçados.
13. SmtpEmailSender trata falhas sem vazar senha: sim, não registra senha SMTP.
14. EmailJobService registra failed/last_error sanitizado: sim.
15. Certificado tem logs sem vazar dados sensíveis: política aplicada; evitar participante completo.
16. AuthService mascara e-mail nos logs: sim.
17. Cutover/rollback tem logs e auditoria: validadores existentes preservados; rollback transacional segue separado.
18. Testes cobrindo error handling/rollback/logs sensíveis: há testes existentes e contratos de validação; ampliar mocks em próxima sprint.
19. Validadores impedem regressão: sim para middleware, correlationId, repository logging, migration logging, health e frontend errors.
20. Pendente para homologação operacional: expandir try/catch real em todos os repositories, SMTP real com timeout, testes unitários mais profundos e remover console legado de scripts não críticos.
