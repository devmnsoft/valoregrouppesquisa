# Sprint 04 — Diagnóstico SaaS Planos, Módulos e Permissões

1. No legado, planos e módulos são definidos por arquivos JavaScript (`role-definitions.js`, `module-definitions.js`, `app.js`) e documentos comerciais; a regra fica distribuída no front/legado.
2. Perfis do legado: `admin_valora`, `consultor_valora`, `empresa_admin`, `gestor_pesquisa`, `analista_resultados`, `gestor_area`, `participante`, `convidado_externo`.
3. Perfis já existentes no backend-v2 antes da sprint: `admin_valora`, `empresa_admin`, `gestor_pesquisa`, `participante`.
4. Módulos do legado/documentos: clientes, planos, módulos, usuários, formulários, pesquisas, links públicos, respostas, relatórios, certificados, convites por e-mail, auditoria, LGPD, suporte, white label, benchmark, exportações, integrações, dashboard e configurações.
5. Faltavam no backend-v2 tabelas, services e bloqueios formais para todos os módulos SaaS; havia apenas fluxo vertical mínimo.
6. `organizations.plan_code` era usado como campo texto simples em organizações, DTOs e seeds; nesta sprint foi mantido por compatibilidade e sincronizado com `subscriptions`.
7. Novas tabelas planejadas: `plans`, `plan_limits`, `plan_capabilities`, `modules`, `organization_modules`, `subscriptions`, `usage_monthly`.
8. Novos endpoints planejados: `/plans`, `/modules`, `/organizations/{id}/subscription`, `/organizations/{id}/usage`, `/organizations/{id}/entitlements`, `/dashboard/*`, `/me/menu`.
9. Endpoints existentes a validar: forms, surveys, survey links, public responses e users.
10. Telas Web: `/Admin/Plans`, `/Admin/Modules`, `/Admin/Subscriptions`, `/Admin/Usage`, dashboard e menu dinâmico.
11. Testes necessários: permissões, entitlement, uso, plano, módulo, assinatura inativa, limites, menu e dashboard seguro.
12. Riscos: preservar fluxo vertical, manter `plan_code`, não expor hashes/tokens e não bloquear seeds existentes por ausência inicial de subscription.
13. Plano: criar modelo SaaS, SQL/seed, services/repositories/controllers, aplicar bloqueios, atualizar Web/README/validadores e registrar auditoria.

## Validação inicial

* `dotnet build backend-v2/ValoraPesquisa.sln`: não executado com sucesso porque o SDK `dotnet` não está instalado no ambiente (`dotnet: command not found`).
* `dotnet test backend-v2/ValoraPesquisa.sln`: não executado com sucesso porque o SDK `dotnet` não está instalado no ambiente (`dotnet: command not found`).
* `npm run backend-v2:validate`: executado com sucesso antes das mudanças; validador foundation OK.
