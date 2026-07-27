# Runbook de homologação da identidade

## Pré-requisitos

.NET SDK 10, Node conforme lockfile, Firebase CLI/emuladores, PostgreSQL acessível por `VALORA_TEST_POSTGRES_CONNECTION`, segredo JWT externo forte e SMTP de homologação.

## Ordem obrigatória

```bash
npm ci
npm run repository:boundaries
npm run security:check
npm run test:rules
npm run check
npm run build:prod
node backend/tools/validation/validate-phase2d-identity-vertical.js
cd backend
dotnet --info
dotnet restore Valora.sln
dotnet build Valora.sln --configuration Release
dotnet test Valora.sln --configuration Release
dotnet format Valora.sln
dotnet format Valora.sln --verify-no-changes
psql "$VALORA_TEST_POSTGRES_CONNECTION" -v ON_ERROR_STOP=1 -f database/postgresql/banco_completo.sql
psql "$VALORA_TEST_POSTGRES_CONNECTION" -v ON_ERROR_STOP=1 -f database/postgresql/banco_completo.sql
```

Aplique também todas as migrations em ordem duas vezes. Homologue cadastro, rollback induzido, login, rotação, detecção de reuso, logout, reset, outbox e isolamento entre dois tenants. Não promova enquanto qualquer comando falhar.

## Rollback operacional

A migration é aditiva e não remove dados. Em incidente, interrompa novos cadastros e consumidores de e-mail, reverta a aplicação e preserve as tabelas para investigação. Não execute `DROP`; uma migration corretiva posterior deve desativar ou ajustar os novos objetos.
