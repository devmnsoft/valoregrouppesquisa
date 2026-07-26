# Revisão Final de Paridade — Legado → Backend Oficial RC2

| Jornada legado | Web oficial | Endpoint API | Service/Repository | Status |
|---|---|---|---|---|
| Home | `Views/Home` | `/public/*`, `/plans` | Public/Plan services | migrado |
| Diagnóstico gratuito | `Views/FreeDiagnostics` | `/free-diagnostics/*` | FreeDiagnosticsService/Repository | migrado |
| Pesquisa pública | `Views/PublicSurvey` | `/public-surveys/*` | PublicSurveyService/SurveyRepository | migrado |
| Resultado | `Views/Results` | `/responses/{id}/result`, `/public-results/*` | ResultService/ResultRepository | migrado |
| Certificado | `Views/Certificates` | `/certificates/*`, `/public-certificates/*` | Certificate services/repositories | migrado |
| E-mail | `Views/Email` | `/email/*` | EmailQueueService/OperationalRepository | migrado |
| Login | `Views/Auth` | `/auth/login`, `/auth/me` | AuthService/UserRepository | migrado |
| Dashboard | `Views/Dashboard` | `/admin/dashboard` | DashboardMetricsService | migrado |
| Usuários | `Views/Users` | `/admin/users` | UserRepository/AuthService | parcial |
| Formulários | `Views/Forms` | `/forms` | FormRepository | migrado |
| Pesquisas | `Views/Surveys` | `/surveys` | SurveyRepository | migrado |
| Respostas | `Views/Responses` | `/responses` | ResponseRepository | migrado |
| Relatórios | `Views/Reports` | `/reports` | ReportService/ReportRepository | migrado |
| LGPD | `Views/Lgpd` | `/lgpd/*` | Lgpd/Privacy services | migrado |
| Comunicação | `Views/Communications` | `/communications` | CommunicationRepository | parcial |
| Auditoria | `Views/Audit` | `/admin/audit` | AuditRepository | parcial |
| Módulos | `Views/Modules` | `/modules` | ModuleRepository/EntitlementService | migrado |
| Planos | `Views/Plans` | `/plans` | PlanRepository | migrado |
| Suporte | Operação/documentação | N/A | N/A | parcial |
| ValoraBot | Referência legada sem equivalente oficial obrigatório | N/A | N/A | não aplicável |

Observação: o legado permanece preservado apenas como referência; a Web oficial não deve usar Firebase nem acessar PostgreSQL diretamente.
