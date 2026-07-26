# Fronteiras do repositório

O repositório mantém somente duas aplicações: o legado JavaScript/Firebase na raiz e a nova plataforma ASP.NET Core 10 em `backend/`.

## Regras automatizadas

Execute na raiz:

```bash
npm run repository:boundaries
```

O validador falha se houver solutions paralelas, `.csproj` fora de `backend/`, banco PostgreSQL fora de `backend/database`, `global.json` fora de `backend/`, documentação .NET solta na raiz, produto externo contaminante ou referências ao projeto predecessor removido.
