# MVP Vertical PostgreSQL

Fluxo entregue para homologação controlada: PostgreSQL → API ASP.NET Core → provider API no frontend → pesquisa pública → resposta → resultado → certificado → job de e-mail.

## Como subir

1. `npm run postgres:up`
2. `dotnet run --project backend/Valora.Api/Valora.Api.csproj`
3. `node scripts/validate-api-backend.js`

Produção continua com `DATA_PROVIDER: 'firebase'` até homologação.
