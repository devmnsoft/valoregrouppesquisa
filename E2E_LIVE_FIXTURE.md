# E2E Live Fixture

Fixture determinística local da Sprint 32 para homologação live.

- Seed: `database/postgresql/099_seed_e2e_live_fixture.sql`.
- Organização: Valora E2E Organization.
- Admin: `e2e-admin@valoragroup.local`.
- Senha somente para teste retornada por `/e2e/fixture`: `Valora!12345`.
- A senha não é persistida em texto puro; o endpoint seguro recalcula hash local pelo hasher da aplicação.
- Plano: `free`, assinatura ativa e limites controlados.
- Pesquisa: Diagnóstico Valora Insight E2E, com 5 dimensões e 25 perguntas.
- Token público de fixture: `e2e-public-token-sprint32`.

## Regras de segurança

- Não usar dados reais.
- Não usar e-mail real de cliente.
- Não rodar seed E2E em produção.
- Endpoints `/e2e/fixture` e `/e2e/reset` são bloqueados quando `ASPNETCORE_ENVIRONMENT=Production`.
- Permitidos apenas em Development, Local, Test ou com `E2E:EnableFixtureEndpoints=true` fora de produção.
