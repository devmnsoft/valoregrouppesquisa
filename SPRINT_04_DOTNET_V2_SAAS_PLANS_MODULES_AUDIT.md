# Sprint 04 — Auditoria Final SaaS Planos, Módulos e Permissões

1. Resumo: a base .NET v2 recebeu a primeira camada SaaS parametrizada por planos, módulos, limites, assinatura, uso mensal, dashboard e menu dinâmico.
2. Diagnóstico inicial: registrado em `SPRINT_04_DOTNET_V2_SAAS_PLANS_MODULES_DIAGNOSTIC.md`.
3. Modelos criados: `Plan`, `PlanLimit`, `PlanCapability`, `Module`, `OrganizationModule`, `Subscription`, `UsageMonthly` e DTOs seguros.
4. Tabelas criadas: `plans`, `plan_limits`, `plan_capabilities`, `modules`, `organization_modules`, `subscriptions`, `usage_monthly`.
5. Seeds criados: planos free/essential/growth/enterprise/internal, módulos comerciais e subscriptions compatíveis com `organizations.plan_code`.
6. Services criados: `PlanService`, `ModuleService`, `SubscriptionService`, `EntitlementService`, `UsageService`, `DashboardMetricsService`, `PermissionService`, `MenuService`.
7. Repositories criados: `PlanRepository`, `ModuleRepository`, `SubscriptionRepository`, `UsageRepository`.
8. Controllers criados: `PlansController`, `ModulesController`, `SubscriptionsController`, `UsageController`, `EntitlementsController`, `DashboardController`, `MenuController`.
9. Endpoints criados: `/plans`, `/modules`, `/organizations/{id}/modules`, `/organizations/{id}/subscription`, `/organizations/{id}/usage`, `/organizations/{id}/entitlements`, `/dashboard/global`, `/dashboard/organization`, `/me/menu`.
10. Bloqueios comerciais aplicados: formulários, pesquisas, publicação, links, respostas públicas e usuários validam módulo/limite/assinatura.
11. Permissões avançadas aplicadas: oito perfis do legado mapeados em `PermissionService`.
12. Dashboard global criado: usa repositórios reais para organizações, usuários, pesquisas, respostas e auditoria.
13. Dashboard da organização criado: retorna plano, assinatura, uso por limite, módulos, respostas e auditoria.
14. Web MVC atualizada: novas actions/views para planos, módulos, assinaturas e uso; JavaScript monta menu dinâmico e cards/progresso.
15. Menu dinâmico criado: endpoint `/me/menu` considera perfil, organização, módulo e assinatura.
16. Testes criados: testes estáticos/unitários de permissão, entitlement, SQL SaaS, menu/dashboard seguro.
17. Validadores criados: `backend-v2/tools/validate-backend-v2-saas.js` e script npm `backend-v2:saas-validate`.
18. Comandos executados: `dotnet build`, `dotnet test`, `npm run backend-v2:validate`, `npm run backend-v2:saas-validate` foram tentados conforme disponibilidade; npm validators passaram.
19. Comandos não executados e motivo: build/test .NET não puderam ser validados porque `dotnet` não está instalado neste container.
20. Gaps restantes: financeiro completo, gateway, SMTP real, WhatsApp, certificados ricos, LGPD completa e importação Firebase total seguem fora do escopo.
21. Riscos: validar compilação em ambiente com SDK .NET; revisar SQL em banco real antes de produção; ajustar granularidade de módulos por plano com produto/comercial.
22. Próximo passo recomendado: Sprint 05 — relatórios, certificados, exportações, LGPD inicial e comunicação por e-mail.
