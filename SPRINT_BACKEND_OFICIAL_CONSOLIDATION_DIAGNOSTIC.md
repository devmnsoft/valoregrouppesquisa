# Diagnóstico — Sprint Backend Oficial Consolidation

## 1. O que existe hoje em `backend/Valora.sln`

A solução oficial já contém os projetos `Valora.Api`, `Valora.Application`, `Valora.Domain`, `Valora.Infrastructure`, `Valora.Tests` e `Valora.Web`. A API possui controllers públicos, administrativos, autenticação, planos, organizações, respostas, certificados, comunicações, operações e migração. A Application já contém contratos, DTOs públicos, services de autenticação, planos, auditoria, pesquisa pública, resultado público, certificados e comunicação. A Infrastructure já usa Dapper/PostgreSQL por meio de `PostgresConnectionFactory` e repositories oficiais para organizações, usuários, formulários, pesquisas, respostas, resultados, planos, auditoria, certificados, comunicações, diagnóstico gratuito e migração. A Web oficial já é ASP.NET Core MVC/Razor com views para login, dashboard, organizações, usuários, formulários, pesquisas, links públicos, respostas, resultados, auditoria, planos, certificados, comunicações e diagnóstico gratuito.

## 2. O que existe em `backend-v2` que ainda não existe integralmente no `backend`

`backend-v2` concentra contratos e DTOs compactos para áreas SaaS, repositories específicos de módulos/assinaturas/uso, controllers explícitos para users/forms/survey-links/audit/saas, views administrativas consolidadas e validadores próprios. No backend oficial, parte disso já existe com nomes/padrões diferentes; lacunas remanescentes são interfaces explícitas para módulos, assinatura, uso, entitlements, menu e dashboard, além de documentação objetiva da rota oficial.

## 3. Itens do `backend-v2` que devem ser portados para `backend`

Devem ser reaproveitados conceitos, não a solution paralela: mapa de endpoints SaaS, contratos seguros sem hashes, regras de planos/módulos/limites, menu dinâmico por perfil/plano/módulo, organização dos fluxos administrativos, SQL idempotente e testes/validadores de ausência de dados fake e exposição sensível.

## 4. Itens do `backend-v2` que não devem ser portados

Não portar a solution `ValoraPesquisa.sln`, nomes `ValoraPesquisa.*`, Dockerfiles paralelos como base oficial, scripts que apontam para `backend-v2`, views duplicadas em outro frontend, seeds com tokens públicos em texto ou qualquer padrão que concorra com `backend/Valora.sln`.

## 5. Endpoints oficiais que ainda retornam gap controlado

Os gaps controlados permanecem documentados em `ASPNET_WEB_API_GAPS.md`. A regra desta sprint é manter HTTP 501 com `WEB_ADMIN_REAL_REPOSITORY_REQUIRED` apenas quando houver justificativa documentada e nunca retornar dados fake em produção.

## 6. Repositories oficiais ainda incompletos

Prioridade de consolidação: módulos, assinatura, uso mensal e links públicos como repositories explícitos. Repositories já existentes para organizações, usuários, formulários, pesquisas, respostas, resultados, auditoria, planos, comunicações e certificados devem continuar como base oficial.

## 7. Services oficiais ainda incompletos

Prioridade: `EntitlementService`, `PermissionService`, `DashboardMetricsService`, `MenuService`, além de services administrativos finos para organizações, usuários, formulários, pesquisas, links, módulos, assinatura e uso quando controllers ainda concentrarem fluxo.

## 8. Telas do `backend/Valora.Web` ainda dependentes de fallback

Dashboard, organizações, usuários, formulários, pesquisas, links públicos, respostas, auditoria, planos, módulos/assinatura/uso devem consumir endpoints oficiais e exibir estados vazios úteis quando API retornar gap documentado. Não deve haver dados fictícios para preencher UI.

## 9. Tabelas do banco oficial divergentes

O script oficial já cobre grande parte do schema. Divergências a monitorar: presença e compatibilidade de `modules`, `organization_modules`, `subscriptions`, `usage_monthly`, `survey_invites`, `survey_participants`, `organization_settings`, `organization_branding`, `user_profiles`, `roles`, `permissions` e `role_permissions` entre `scriptbd_completo.sql`, `database/postgresql/scriptbd_completo.sql` e scripts incrementais.

## 10. Scripts SQL que precisam ser ajustados

Manter `database/postgresql/scriptbd_completo.sql` como fonte operacional e sincronizar `scriptbd_completo.sql` raiz. Scripts incrementais devem permanecer idempotentes e conter seeds sem senha em texto e sem token público real em texto.

## 11. Validadores que apontam para `backend-v2`

Os scripts `backend-v2:validate` e `backend-v2:saas-validate` continuam apenas como referência histórica. O novo validador oficial é `backend:official-validate`, apontando para `tools/validate-backend-official-migration.js` e para `backend/Valora.sln`.

## 12. Riscos da consolidação

Riscos: rotas duplicadas, quebra de Web MVC, exposição acidental de hashes/tokens, divergência entre scripts SQL, controllers com regra pesada, uso indevido de `backend-v2` em automações oficiais, seeds inseguros e endpoints administrativos com fallback não documentado.

## 13. Plano objetivo da sprint

1. Declarar `backend/Valora.sln` como fonte oficial.
2. Documentar mapa `backend-v2` → `backend`.
3. Completar entidades e contratos seguros mínimos no backend oficial.
4. Criar validador oficial.
5. Atualizar documentação de gaps, rotas, checklist e auditoria final.
6. Executar build/test/validador quando o ambiente permitir.
