# Sprint XX — Diagnóstico de Repositories Administrativos Reais

## 1. Endpoints administrativos que ainda retornam 501

Nenhum endpoint principal listado para esta sprint retorna `WEB_ADMIN_REAL_REPOSITORY_REQUIRED` no `WebAdminModulesController`. O controller injeta repositories reais para organização, usuários, formulários, pesquisas, respostas, auditoria e planos.

## 2. Endpoints com dados temporários ou não definitivos

- `POST /users` gera senha temporária aleatória e persiste apenas o hash; o fluxo de convite/entrega de senha ainda deve ser formalizado por e-mail transacional.
- `GET /organization/current/limits` depende de `PlanRepository`; a auditoria encontrou divergência de schema no `scriptbd_completo.sql` para `plan_limits`, `plan_capabilities` e `subscriptions`, corrigida nesta sprint.

## 3. Repositories existentes

- `OrganizationRepository`
- `UserRepository`
- `FormRepository`
- `SurveyRepository`
- `ResponseRepository`
- `AuditRepository`
- `PlanRepository`
- `ResultRepository`
- `CertificateRepository`
- `CommunicationRepository`
- `FreeDiagnosticsRepository`
- `MigrationRepository`

## 4. Repositories que precisam ser corrigidos

- `PlanRepository`: não exigiu alteração C#, mas exigiu correção no schema completo para a forma real usada pelo repository (`limit_key`, `limit_value`, `capability_key`, `capability_level`).
- `OrganizationRepository`: exigiu alinhamento do script completo com colunas lidas/escritas (`document`, `email`, `phone`, `settings_json`, `brand_json`).

## 5. Repositories que precisam ser criados

Nenhum repository novo foi necessário para retirar os endpoints principais do gap controlado, porque os módulos solicitados já possuem repositories reais no projeto.

## 6. Colunas usadas pelos repositories mas ausentes do `scriptbd_completo.sql`

Antes da correção, o script completo não declarava em `organizations`:

- `document`
- `email`
- `phone`
- `settings_json`
- `brand_json`

Também havia incompatibilidade de tipo/shape para tabelas usadas por `PlanRepository`:

- `plan_limits.limit_key`
- `plan_limits.limit_value`
- `plan_capabilities.capability_key`
- `plan_capabilities.capability_level`
- `plan_capabilities.capability_type`
- `subscriptions.plan_id text`
- `subscriptions.started_at`, `trial_ends_at`, `cancelled_at`, `billing_status`

## 7. Colunas em `database/postgresql/*.sql` ausentes no `scriptbd_completo.sql`

Foram reconciliadas no script completo:

- `organizations.document`
- `organizations.email`
- `organizations.phone`
- `organizations.settings_json`
- `organizations.brand_json`
- `plan_limits.limit_key`
- `plan_limits.limit_value`
- `plan_capabilities.capability_key`
- `plan_capabilities.capability_level`
- `plan_capabilities.capability_type`
- `subscriptions.started_at`
- `subscriptions.trial_ends_at`
- `subscriptions.cancelled_at`
- `subscriptions.billing_status`

## 8. DTOs faltantes

Não foi identificado bloqueio de compilação por DTO faltante para os endpoints principais. O controller administrativo ainda aceita alguns payloads via `JsonElement`; uma próxima sprint pode tipar requests/responses administrativos específicos para reduzir ambiguidade.

## 9. Services/use cases faltantes

Os endpoints ainda orquestram diretamente repositories no controller administrativo central. Isso remove o 501 e usa banco real, mas o próximo endurecimento arquitetural deve extrair use cases por módulo administrativo.

## 10. Testes que precisam ser criados

Criados/ajustados testes estáticos em `AdminRepositoryMigrationTests` para:

- garantir ausência de `WEB_ADMIN_REAL_REPOSITORY_REQUIRED` e `StatusCode(501)` no controller administrativo;
- garantir colunas de organização esperadas no script completo;
- garantir shape de planos compatível com `PlanRepository`;
- garantir que listagens administrativas não selecionam hashes sensíveis.
