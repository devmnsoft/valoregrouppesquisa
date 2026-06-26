# MVP Vertical PostgreSQL

Entregue como arquitetura paralela: Docker PostgreSQL 16 na porta 5434, API ASP.NET Core em `backend/Valora.sln`, seed dos planos oficiais e demo Valora Insight™. Produção permanece em Firebase via `DATA_PROVIDER: 'firebase'`.

## Execução
1. `npm run postgres:up`
2. `dotnet run --project backend/Valora.Api`
3. `npm run api:provider && npm run journey:provider`
