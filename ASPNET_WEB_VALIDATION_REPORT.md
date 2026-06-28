# Valora.Web — relatório de validação

Data: 2026-06-28.

## Validações executadas neste ambiente

- `npm run web:validate`: OK.
- `npm run web:jquery`: OK.
- `npm run web:api-contract`: OK.
- `npm run web:screens`: OK.
- `npm run web:errors`: OK.
- `npm run web:mobile`: OK.
- `npm run web:cors`: OK.
- `npm run web:docker`: OK.
- `npm run web:no-binaries`: OK.
- `npm run web:functional-flow`: OK.

## Limitações do ambiente

- `npm run web:build` não pôde concluir porque `dotnet` não está instalado no container.
- E2E live requer Valora.Api, Valora.Web e PostgreSQL ativos nas portas documentadas.

## Observações técnicas

- O frontend ASP.NET consome a API somente via HTTP e `$.ajax`.
- Token de autenticação permanece em `sessionStorage`.
- Erros normalizados exibem `correlationId` e não exibem stack trace.
- A política sem binários foi preservada.
