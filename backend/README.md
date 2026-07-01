# Valora Backend Oficial

`backend/Valora.sln` é a base única e oficial da nova versão .NET do Valora.

## Projetos

- `Valora.Api`: API oficial.
- `Valora.Application`: DTOs, contratos e regras de aplicação.
- `Valora.Domain`: entidades e tipos de domínio.
- `Valora.Infrastructure`: Dapper, PostgreSQL, repositories, segurança e e-mail.
- `Valora.Tests`: testes oficiais.
- `Valora.Web`: front oficial ASP.NET Core MVC/Razor com Bootstrap 5, JavaScript puro e jQuery/AJAX.

## Banco

A persistência oficial usa PostgreSQL, Dapper, schema `valorapesquisa` e scripts em `database/postgresql`.

## Referência temporária

`../backend-v2` permanece preservado somente como referência histórica/técnica. Não implemente novas features nele.

## Comandos

```bash
dotnet build backend/Valora.sln
dotnet test backend/Valora.sln
npm run backend:official-validate
```
