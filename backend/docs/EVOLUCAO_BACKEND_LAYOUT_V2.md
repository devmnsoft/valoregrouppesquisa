# Evolução Backend/Layout V2 — auditoria rápida e decisões

## Auditoria técnica rápida

- `functions/index.js`: concentra o legado Firebase callable com pesquisa pública, resultado público, usuários, clientes, convites, e-mails, suporte, pagamentos e rotinas agendadas. Deve continuar intacto durante a consolidação.
- `backend/Valora.Api`: já expõe API REST versionada para auth, pesquisas, respostas, resultados, certificados, relatórios, exportações, LGPD, comunicação, planos, usuários, acesso e organização. Nesta entrega ganhou endpoints reais para unidades e setores.
- `backend/Valora.Application`: possui serviços de entitlement, planos, dashboard, certificados, relatórios, e-mail, LGPD e administração. Nesta entrega ganhou serviço de estrutura organizacional com bloqueio por plano.
- `backend/Valora.Domain`: já contém entidades de `Unit`, `Department`, `Survey`, `Certificate`, `AuditLog`, `EmailJob`, `LgpdConsent` e planos, confirmando que a modelagem de matriz/filial/setor já estava prevista.
- `backend/Valora.Infrastructure`: contém repositórios Dapper/PostgreSQL. Nesta entrega ganhou repositório de unidades e setores usando o schema canônico `valorapesquisa`.
- `backend/Valora.Web`: possui MVC/BFF com layout administrativo e público. Nesta entrega conectou a tela Organização 360 aos novos fluxos de unidades/setores.
- `backend/Valora.Tests`: cobre planos, auth, certificados, LGPD, e-mail, pesquisa pública e contratos de API; ainda precisa ampliar cobertura dos novos endpoints com banco de integração.
- `src` e `public`: não existem neste snapshot; o frontend ativo está em `backend/Valora.Web/wwwroot` e Views Razor.

## Matriz de paridade Firebase Functions x .NET

| Função | Firebase | API .NET | Application | Infrastructure | Tela Web/React | Teste | Produção |
|---|---:|---:|---:|---:|---:|---:|---|
| getPlanCatalog | Parcial/legado | Sim (`Plans`) | Sim | Sim | Sim | Sim | alinhar nomes legados |
| getCompanyUsage | Parcial | Sim (`organization/current/usage`) | Sim | Sim | Sim | Sim | expandir métricas |
| updateSubscriptionPlan | pagamento/assinatura | Sim | Sim | Sim | Sim | Parcial | conciliar webhooks |
| createUserByAdmin | `createUser` | Sim | Sim | Sim | Sim | Parcial | auditoria granular |
| createClientByMember | `createClient` | Parcial | Parcial | Sim | Sim | Parcial | regras por perfil |
| updateClientByMember | `updateClient` | Parcial | Parcial | Sim | Sim | Parcial | escopo multiempresa |
| submitSurveyResponse | Sim | Sim | Sim | Sim | Sim | Sim | equivalência total de payload |
| getSurveyResult | `getPublicResult` | Sim | Sim | Sim | Sim | Sim | tokens e expiração auditáveis |
| getCompanyAnalysis | Parcial | Dashboard/Results | Sim | Sim | Sim | Parcial | filtros unidade/setor |
| getCompanySettings | settings/org | Sim | Sim | Sim | Sim | Parcial | chaves seguras |
| updateCompanySettings | settings/org | Sim | Sim | Sim | Sim | Parcial | versionamento |
| updateMemberAdminStatus | admin users | Sim | Sim | Sim | Sim | Parcial | auditoria |
| disableUser | admin CRUD | Sim | Sim | Sim | Sim | Parcial | motivo obrigatório |
| reactivateUser | admin CRUD | Sim | Sim | Sim | Sim | Parcial | motivo obrigatório |
| deleteUserCompletely | admin CRUD | Parcial | Parcial | Sim | Sim | Parcial | preferir LGPD/soft delete |
| inviteMember | invite | Sim | Sim | Sim | Sim | Parcial | e-mail e expiração |
| acceptInvite | invite | Parcial | Parcial | Sim | Sim | Parcial | UX pública |
| resendInvite | invite | Sim | Sim | Sim | Sim | Parcial | rate limit |
| cancelInvite | invite | Sim | Sim | Sim | Sim | Parcial | motivo/auditoria |
| removeMemberFromCompany | admin CRUD | Sim | Sim | Sim | Sim | Parcial | escopos |
| changeMemberRole | admin CRUD | Sim | Sim | Sim | Sim | Sim | matriz efetiva |
| exportCompanyData | `exportData` | Sim | Sim | Sim | Sim | Parcial | filtros avançados |
| generateCertificate | certificado | Sim | Sim | Sim | Sim | Sim | histórico completo |
| downloadCertificate | certificado | Sim | Sim | Sim | Sim | Sim | token temporário |
| generateReportPdf | relatórios | Sim | Sim | Sim | Sim | Parcial | identidade PDF final |
| submitFeedback | suporte/ticket | Parcial | Parcial | Sim | Sim | Parcial | painel dedicado |
| adminListFeedbacks | suporte/ticket | Parcial | Parcial | Sim | Sim | Parcial | SLA/status |
| migrateUserToCompanyMember | repair/migration | Parcial | Parcial | Sim | Sim | Parcial | migração assistida |

## Decisões desta entrega

1. Não remover ou alterar contratos Firebase legados.
2. Consolidar uma fatia funcional real no .NET: estrutura organizacional (unidades e setores), pois já existia no domínio/schema e desbloqueia comparativos por unidade/setor.
3. Aplicar entitlement antes de criar unidade/setor, usando mensagem amigável e segura.
4. Conectar a funcionalidade à tela Organização 360 com estados de carregamento, vazio, ação, confirmação destrutiva e responsividade.
