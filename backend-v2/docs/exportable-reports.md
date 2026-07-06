# exportable reports

## Objetivo

Documento v1.1 pós-piloto para estabilizar homologação e evoluir o MVP sem recriar o sistema.

## Escopo entregue

- Migration idempotente para schema integrarp.
- Modelos de domínio para formulários, automações, anexos, notificações e relatórios.
- Contratos de aplicação com Result<T>, CancellationToken, ILogger<T> e validação de permissões.
- Endpoints API v1.1 protegidos por autorização.
- Assets Web para Form Builder, preview, automações, anexos, notificações e relatórios.

## Limitações conhecidas

- XLSX e PDF real ficam documentados para evolução quando houver dependência já aprovada.
- Push externo real não foi implementado; v1.1 usa canal fake/local.
- Validação local de .NET e Docker depende das ferramentas instaladas no ambiente.
