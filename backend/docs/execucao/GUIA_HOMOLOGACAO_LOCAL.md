# Guia de homologação local

## Pré-requisitos

- .NET SDK indicado em `global.json`;
- PostgreSQL 16+ com `psql` no `PATH`;
- portas locais 5080, 5088 e 5434 disponíveis (ou configurações equivalentes).

## Preparação no Windows

1. Copie as chaves de `.env.example` para variáveis da sessão; não crie um `.env` versionado.
2. Ajuste `ConnectionStrings__DefaultConnection` para seu PostgreSQL.
3. Execute `dotnet restore Valora.sln` e `dotnet build Valora.sln --configuration Release` em `backend/`.
4. Defina `ASPNETCORE_ENVIRONMENT=Development` e execute `database/postgresql/apply-local.ps1`.
5. Para massa sintética, defina também `VALORA_SEED_DEMO=true` antes do passo 4. O wrapper bloqueia o seed fora de Development.
6. Execute `run-local.bat`, acesse `http://localhost:5088/Account/Login` e use `admin.demo@valora.local` / `Valora!12345` apenas localmente.

Linux/macOS usam `database/postgresql/apply-local.sh` e `run-local.sh`. Acesse `/EnvironmentStatus` para validar API, PostgreSQL, schema, SMTP, armazenamento e versão sem expor credenciais.

## Fluxo público

No Estúdio de Diagnósticos, crie ou abra um ciclo, publique-o e copie o link público persistido. Abra o link em janela anônima, aceite o consentimento LGPD e envie uma resposta. O Dashboard só marca prontidão após os dados reais aparecerem nas APIs.
