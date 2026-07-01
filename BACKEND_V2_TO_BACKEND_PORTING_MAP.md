# Mapa Técnico — `backend-v2` → `backend`

## Auth

- **Já existe em `backend`:** `AuthController`, `AuthService`, `IUserRepository`, `IPasswordHasher`, `IJwtTokenService`, DTOs de login/registro/reset.
- **Feito em `backend-v2`:** fluxo compacto de login/me/menu em controllers e contratos.
- **Portar:** semântica de `/me/menu`, contratos seguros e testes de não exposição de hashes.
- **Não portar:** namespace `ValoraPesquisa.*`, solution paralela e qualquer armazenamento de token em texto.

## Organizações

- **Entidades:** `Organization` existe; criadas referências oficiais para `OrganizationSettings` e `OrganizationBranding`.
- **DTOs:** `OrganizationDto` seguro consolidado.
- **Repositories:** `OrganizationRepository` já existe; manter Dapper parametrizado e isolamento por `organization_id`.
- **Services:** evoluir `OrganizationService` quando controllers concentrarem regra.
- **Controllers:** `OrganizationsController` oficial existe.
- **Views/JS:** `backend/Valora.Web/Views/Organization` deve consumir API oficial.
- **SQL:** garantir `organizations`, settings e branding nos scripts oficiais.

## Usuários

- **Entidades:** `User` existe; criados `UserProfile`, `Role`, `Permission`, `RolePermission`.
- **DTOs:** `UserDto` sem `password_hash`.
- **Repositories:** `UserRepository` oficial existe para hash internamente, sem expor DTO sensível.
- **Services:** `AuthService` existe; evoluir `UserService` para CRUD administrativo.
- **Controllers:** endpoints de usuários devem ficar em `backend/Valora.Api`.
- **Views/JS:** `Views/Users` oficial.
- **SQL:** `users`, `roles`, `permissions`, `role_permissions`, `user_profiles`.

## Formulários

- **Entidades:** `Form`, `FormDimension`, `Question`, `QuestionOption` já existem.
- **DTOs:** `FormDto`, `QuestionDto`, `QuestionOptionDto`.
- **Repositories:** `FormRepository` oficial existe.
- **Services:** criar/refinar `FormService` se regra estiver no controller.
- **Controllers:** endpoints oficiais de forms em `Valora.Api`.
- **Views/JS:** `Views/Forms` e JS específico.
- **SQL:** `forms`, `form_dimensions`, `questions`, `question_options`.

## Pesquisas

- **Entidades:** `Survey` existe.
- **DTOs:** `SurveyDto`.
- **Repositories:** `SurveyRepository` oficial existe.
- **Services:** `PublicSurveyService` existe; criar `SurveyService` administrativo quando necessário.
- **Controllers:** `SurveysController`, `PublicSurveysController`.
- **Views/JS:** `Views/Surveys`.
- **SQL:** `surveys` com status/soft delete e tokens sempre armazenados como hash.

## Links públicos

- **Entidades:** `SurveyLink` existe.
- **DTOs:** `SurveyLinkDto` sem `token_hash`.
- **Repositories:** contrato `ISurveyLinkRepository` criado para consolidação explícita.
- **Services:** criar `SurveyLinkService` centralizando geração/revogação.
- **Controllers:** endpoints `/surveys/{surveyId}/links` e `/survey-links/{linkId}/status`.
- **Views/JS:** `Views/Surveys/PublicLinks`.
- **SQL:** `survey_links` com `token_hash`/`public_token_hash` nunca expostos.

## Respostas

- **Entidades:** `SurveyResponse` e `ResponseAnswer` existem; criada entidade alias segura `Response`.
- **DTOs:** `ResponseDto` sem `result_token_hash`.
- **Repositories:** `ResponseRepository` oficial existe.
- **Services:** `PublicResponseTransactionService`, submitter e normalizer existem.
- **Controllers:** `ResponsesController`, submit público.
- **Views/JS:** `Views/Responses`.
- **SQL:** `responses`, `response_answers`.

## Resultados

- **Entidades:** `ResultScore`, `DimensionScore` existem.
- **DTOs:** DTOs existentes e `ResultScoreDto`/`DimensionScoreDto`.
- **Repositories:** `ResultRepository` oficial existe.
- **Services:** `PublicResultService`, calculator e devolutiva existem.
- **Controllers:** `PublicResultsController`.
- **Views/JS:** `Views/Results`.
- **SQL:** `result_scores`, `dimension_scores`.

## Auditoria

- **Entidades:** `AuditLog` existe.
- **DTOs:** `AuditEventDto`.
- **Repositories:** `AuditRepository` oficial existe.
- **Services:** `AuditService` existe.
- **Controllers:** rota `/audit/events` deve usar repository real ou gap documentado.
- **Views/JS:** `Views/Audit`.
- **SQL:** `audit_logs`.

## Planos e módulos

- **Entidades:** `Plan`, `PlanLimit`, `PlanCapability` existem; criados `Module` e `OrganizationModule`.
- **DTOs:** `PlanDto` existente e `ModuleDto`.
- **Repositories:** `PlanRepository` existe; criado contrato `IModuleRepository`.
- **Services:** `PlanEntitlementService` existe; evoluir `ModuleService`.
- **Controllers:** `PlansController` e endpoints de módulos/organização.
- **Views/JS:** `Views/Plans`; telas de módulos devem permanecer MVC/Razor.
- **SQL:** `plans`, `plan_limits`, `plan_capabilities`, `modules`, `organization_modules`.

## Assinatura e limites

- **Entidades:** `Subscription`, `UsageMonthly` existem.
- **DTOs:** `SubscriptionDto`, `UsageDto` existente.
- **Repositories:** contratos `ISubscriptionRepository` e `IUsageRepository` criados.
- **Services:** `IEntitlementService` criado; centralizar limites.
- **Controllers:** endpoints `/organizations/{id}/subscription`, `/usage`, `/entitlements`, `/can-use`.
- **Views/JS:** telas oficiais de assinatura/uso sem dados fake.
- **SQL:** `subscriptions`, `usage_monthly`.

## Dashboard

- **Já existe:** Web dashboard oficial.
- **Portar:** service/contrato `IDashboardMetricsService` para métricas reais.
- **Não portar:** cards estáticos/fake do `backend-v2`.

## Menu dinâmico

- **Já existe:** layout/sidebar do `Valora.Web`.
- **Portar:** contrato `IMenuService` e regra por perfil/plano/módulo.
- **Não portar:** menu hardcoded como fonte de autorização.

## LGPD, certificados, e-mail e relatórios

- **Já existe:** certificados, comunicações, e-mail jobs, diagnóstico gratuito e operações.
- **Falta/evoluir:** LGPD inicial completa, exportações, relatórios avançados e comunicação por e-mail na próxima sprint, mantendo secrets fora de UI/logs.
