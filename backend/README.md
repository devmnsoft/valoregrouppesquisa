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

## Homologação local no Windows

1. Instale o SDK indicado por `global.json`, PostgreSQL 16+ (incluindo `psql`) e confirme com `dotnet --info` e `psql --version`.
2. Abra PowerShell em `backend/` e restaure/compile com `dotnet restore Valora.sln` e `dotnet build Valora.sln --configuration Release`.
3. Crie um banco vazio. O exemplo local versionado usa porta `5434`, banco `valoradb`, usuário `valora` e senha **somente de desenvolvimento** `valora_dev_123`; ajuste-o à sua instalação.
4. Defina a conexão apenas na sessão atual:

   ```powershell
   $env:ASPNETCORE_ENVIRONMENT = "Development"
   $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5434;Database=valoradb;Username=valora;Password=valora_dev_123;Search Path=valorapesquisa,public"
   ```

5. Aplique o bootstrap idempotente com `./database/postgresql/apply-local.ps1`. O script pode ser executado novamente e interrompe no primeiro erro SQL.
6. Para uma demonstração controlada, antes do passo anterior defina `$env:VALORA_SEED_DEMO = "true"`. O wrapper recusa o seed fora de `Development`. Ele cria somente a organização **Organização Demo Valora [DEMO]**, estrutura sintética, assinatura Professional temporária e `admin.demo@valora.local` / `Valora!12345`. Troque a senha em qualquer ambiente compartilhado. A flag permanece `false` por padrão e nunca deve ser habilitada em produção.
7. Execute `./run-local.bat`. A API abre em `http://localhost:5080` e o Web em `http://localhost:5088`; acesse `/Account/Login`.
8. No Dashboard, abra a jornada guiada, revise Organização, Diagnósticos e o diagnóstico oficial. Publique, copie o link na área de links públicos e valide-o em uma janela anônima, incluindo consentimento LGPD, envio e tela final.

No Linux/macOS, use as mesmas variáveis, `./database/postgresql/apply-local.sh` e `./run-local.sh`.

## Configuração e produção

Use variáveis de ambiente ou arquivos não versionados. `appsettings.json` não contém conexão operacional e o seed demo fica desligado em todas as configurações versionadas. Em produção são obrigatórios, no mínimo, `ConnectionStrings__DefaultConnection` e um `Jwt__Secret` exclusivo com 32 ou mais caracteres. Nunca reutilize as credenciais de desenvolvimento ou demonstração.

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

## Validação do fluxo de homologação

Com API e Web ativos, valide com dados persistidos: login, Dashboard/onboarding, organização e estrutura, criação/publicação, link público, LGPD/resposta, participação, processamento, módulos de inteligência, Action, Journey/Evolution, entregáveis, relatório/certificado, notificações, governança/auditoria, planos/uso e saúde. Estados sem evidência devem permanecer honestos; o seed não fabrica resultados, inferências ou recomendações como se fossem observações reais.
