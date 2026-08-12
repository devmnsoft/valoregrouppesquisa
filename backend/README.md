# Backend Valora Insight™ ASP.NET Core 10

## Objetivo

Nova plataforma oficial do Valora Insight™ em ASP.NET Core 10, centralizada em `backend/`, para substituir gradualmente o legado JavaScript/Firebase após homologação e cutover controlados.

## Arquitetura

A solution `Valora.sln` segue Clean Architecture com separação entre API, aplicação, domínio, infraestrutura, web e testes. O backend oficial não deve usar Firebase em runtime; integrações com o legado existem apenas para migração, comparação ou documentação.

## Projetos

- `Valora.Api`: endpoints HTTP.
- `Valora.Application`: casos de uso, contratos e DTOs.
- `Valora.Domain`: entidades, regras e objetos de domínio.
- `Valora.Infrastructure`: persistência PostgreSQL com Dapper e serviços externos.
- `Valora.Web`: interface Razor oficial do backend.
- `Valora.Tests`: testes unitários, arquitetura, contratos e banco.

## Requisitos e SDK

- SDK .NET definido em `global.json`.
- Target framework dos projetos: `net10.0`.
- PostgreSQL para homologação real do banco.

## Banco

- Schema oficial: `valorapesquisa`.
- Bootstrap canônico: `backend/database/postgresql/script_completo.sql`.
- Atualizações: reaplique o mesmo `backend/database/postgresql/script_completo.sql`; não há migrations incrementais ativas.
- Seeds: `backend/database/postgresql/seeds/`.
- Validações SQL: `backend/database/postgresql/validation/`.

## Configuração

Use variáveis de ambiente ou arquivos `appsettings.*.json` sem segredos reais versionados. Configure a connection string PostgreSQL antes de executar API, Web ou testes de integração.

## Build e testes

```bash
dotnet restore Valora.sln
dotnet build Valora.sln --configuration Release
dotnet test Valora.sln --configuration Release
dotnet format Valora.sln --verify-no-changes
```

## Execução

Linux/macOS: `./run-local.sh`. Windows: `run-local.bat`. Os scripts iniciam a API em `http://localhost:5080`, aguardam uma resposta saudável de `/health` e somente então iniciam o Web em `http://localhost:5088`/`https://localhost:7088`. Se a API não ficar pronta em 60 segundos, o script encerra com erro em vez de abrir um Web incapaz de autenticar. Para diagnosticar a dependência sem tentar autenticar, consulte `GET /health/web/api` no Web; detalhes de URL são exibidos apenas em Development.

## Migrations

O bootstrap e todas as convergências usam somente `backend/database/postgresql/script_completo.sql`, de forma transacional e idempotente.

## Documentação

A documentação oficial do backend está em `docs/`, organizada por requisitos, arquitetura, banco, migração, operações, segurança, auditorias e arquivo histórico.

## Segurança

Não versionar secrets. Não retornar sucesso para funcionalidades não implementadas. Pendências devem usar erro controlado, código estável e status HTTP apropriado.

## Pendências

Autenticação multiempresa, CNPJ, grupos econômicos, unidades, setores, planos, entitlements, assinaturas, limites transacionais e permissões reais ficam para fase posterior.
