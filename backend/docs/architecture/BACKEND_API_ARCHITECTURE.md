# Backend Api Architecture

Sprint 24 — Homologação Operacional.

## Política
- Logs técnicos usam Serilog/ILogger com propriedades estruturadas e CorrelationId.
- Auditoria de negócio usa `valorapesquisa.audit_logs` e não armazena erro técnico bruto.
- Dados sensíveis devem ser mascarados por `LogSanitizer`: senha, tokens, CPF/documento, telefone, e-mail, private_key, SMTP password e connection string.
- Information: início/sucesso de fluxo relevante. Warning: entrada inválida, token inválido, recurso não encontrado esperado e rollback executado. Error: exceção inesperada, banco, integração, e-mail e migração. Debug: apenas Development.

## Homologação
Executar validadores npm, `dotnet build backend/Valora.sln`, `dotnet test backend/Valora.sln`, `npm run build:prod` e `npm run prod:health`.
