# Testes executados — Sprint 15

- `npm run check`: passou.
- `npm run backend:implementation`: passou.
- `npm run backend:clean`: passou.
- `npm run postgres:schema`: passou.
- `npm run runtime:docker-windows`: passou.
- `dotnet build backend/Valora.sln`: não executado porque `dotnet` não está instalado no container.
- `dotnet test backend/Valora.sln`: não executado pelo mesmo motivo.

Produção permanece preservada em Firebase com `DATA_PROVIDER=firebase` e `ALLOW_API_PRODUCTION_CUTOVER=false` por padrão.
