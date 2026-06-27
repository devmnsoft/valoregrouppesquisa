# Política Sprint 25

- Logging operacional deve ser implementação real, nunca comentário para satisfazer validador.
- Logs não devem expor senha, token puro, hash de token, CPF, telefone, e-mail completo, connection string, private key ou credenciais SMTP.
- Repositories Dapper críticos devem usar `ILogger<T>`, `try/catch`, `logger.LogError(ex, ...)` e `throw;`.
- Validadores devem remover comentários antes de procurar evidências de implementação.
- Erros de API devem retornar resposta amigável com `traceId` e `correlationId`, sem stack trace em produção.
- Homologação operacional exige executar validadores Node, build/testes .NET, build frontend e health checks.
