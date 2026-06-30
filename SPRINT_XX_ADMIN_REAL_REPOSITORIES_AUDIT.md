# Sprint XX — Auditoria Final de Repositories Administrativos Reais

## 1. Resumo do que foi migrado

A sprint consolidou os endpoints administrativos principais já ligados a repositories reais e corrigiu a divergência de schema que impedia uso confiável em produção, principalmente nos dados de organização e planos/limites.

## 2. Arquivos alterados

- `scriptbd_completo.sql`
- `database/postgresql/scriptbd_completo.sql`
- `database/postgresql/002_core_tables.sql`
- `backend/Valora.Tests/AdminRepositoryMigrationTests.cs`
- `SPRINT_XX_ADMIN_REAL_REPOSITORIES_DIAGNOSTIC.md`
- `SPRINT_XX_ADMIN_REAL_REPOSITORIES_AUDIT.md`

## 3. Endpoints que saíram de 501

Os endpoints principais abaixo não possuem mais retorno `WEB_ADMIN_REAL_REPOSITORY_REQUIRED` no controller administrativo:

- `GET /organization/current`
- `PUT /organization/current`
- `GET /organization/current/usage`
- `GET /organization/current/limits`
- `GET /users`
- `POST /users`
- `PUT /users/{userId}`
- `PATCH /users/{userId}/status`
- `GET /forms`
- `GET /forms/{formId}`
- `POST /forms`
- `PUT /forms/{formId}`
- `GET /surveys`
- `GET /surveys/{surveyId}`
- `POST /surveys`
- `PUT /surveys/{surveyId}`
- `PATCH /surveys/{surveyId}/status`
- `GET /surveys/{surveyId}/links`
- `POST /surveys/{surveyId}/links`
- `PATCH /survey-links/{linkId}/status`
- `GET /responses`
- `GET /responses/{responseId}`
- `GET /audit/events`
- `GET /settings`
- `PUT /settings`

## 4. Endpoints que continuam como gap controlado

Nenhum endpoint principal desta sprint permanece como `WEB_ADMIN_REAL_REPOSITORY_REQUIRED`. Gaps arquiteturais restantes são tipagem de requests/responses administrativos e extração de use cases dedicados.

## 5. Repositories implementados

Foram mantidos os repositories reais existentes: `OrganizationRepository`, `UserRepository`, `FormRepository`, `SurveyRepository`, `ResponseRepository`, `AuditRepository` e `PlanRepository`.

## 6. Services/DTOs criados

Nenhum service/DTO novo foi necessário nesta etapa. Os testes documentam que ainda é recomendável substituir payloads administrativos com `JsonElement` por DTOs tipados.

## 7. Ajustes de banco

- `organizations` no script completo passou a incluir `document`, `email`, `phone`, `settings_json` e `brand_json`.
- `plan_limits` no script completo passou para o formato `plan_id`, `limit_key`, `limit_value` usado pelo `PlanRepository`.
- `plan_capabilities` no script completo passou para `capability_key`, `capability_level` e `capability_type`.
- `subscriptions` no script completo passou a usar `plan_id text` e colunas compatíveis com os scripts modulares.
- Seeds de limites/capacidades foram atualizados para o novo shape.
- Scripts modulares de core receberam metadados de exclusão lógica (`deleted_at`, `deleted_by`) em tabelas essenciais.

## 8. Testes criados

Foram adicionados testes estáticos de compatibilidade de schema e segurança em `AdminRepositoryMigrationTests`.

## 9. Comandos executados

- `rg -n "WEB_ADMIN_REAL_REPOSITORY_REQUIRED|501|NotImplemented|TODO|fake|placeholder|Bootstrap API-first|Executar ação" ...`
- `git diff -- scriptbd_completo.sql database/postgresql/scriptbd_completo.sql database/postgresql/002_core_tables.sql`
- `npm run backend:build`
- `npm run backend:test`
- `npm run web:build`
- `npm run web:aspnet-only`
- `npm run web:no-data-access`
- `npm run web:no-fake-admin-data`
- `npm run web:real-api-usage`
- `npm run web:no-json-dump`
- `npm run web:no-sensitive-ui`
- `npm run web:result-token-safety`
- `npm run web:permission-parity`
- `npm run web:module-parity`
- `npm run web:journey-parity`
- `npm run migration:api-parity`
- `npm run migration:web-parity`
- `npm run db:scriptbd-completo`

## 10. Comandos não executados e motivo

Todos os validadores obrigatórios foram tentados. Falhas, se houver, estão registradas na resposta final com a saída/resumo do ambiente.

## 11. Riscos restantes

- Controller administrativo central ainda concentra orquestração que deveria migrar para services/use cases tipados.
- `POST /users` ainda precisa de fluxo transacional de convite/entrega de senha temporária.
- Testes integrados com PostgreSQL real devem ser ampliados para validar CRUD completo em banco efêmero.

## 12. Próximo passo recomendado

Extrair use cases administrativos tipados por módulo e adicionar suíte de integração com PostgreSQL real para validar isolamento por `organizationId`, auditoria e ausência de vazamento de dados sensíveis em respostas serializadas.
