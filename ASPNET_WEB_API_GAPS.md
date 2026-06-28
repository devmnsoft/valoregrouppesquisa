# ASP.NET Web API Gaps

## A. Endpoints existentes e já consumidos
- GET /me — Dashboard/Settings.
- GET /health, GET /health/database, GET /health/migration — Dashboard/Status/Migração.
- GET /admin/email-jobs, POST /admin/email-jobs/process — Comunicações.

## B. Endpoints existentes e ainda não consumidos
- Rotas públicas de surveys, results e certificates continuam disponíveis para jornada pública.

## C. Endpoints faltantes para produto SaaS final
| módulo | tela afetada | endpoint sugerido | método HTTP | payload esperado | resposta esperada | prioridade | bloqueia produção? |
|---|---|---|---|---|---|---|---|
| Formulários | Forms | /forms/{formId}/questions | POST/PUT | perguntas e opções | formulário completo | Alta | sim |
| Auditoria | Audit | /audit/events/export | GET | filtros | arquivo/stream controlado | Média | não |
| Configurações | Settings | /settings/password | PUT | senha atual e nova | ok | Alta | sim |
| Notificações | Settings | /settings/notifications | PUT | preferências | ok | Média | não |

## Endpoints mínimos Sprint 37
Criados aliases funcionais para /organization/current, /users, /forms, /surveys, /survey-links, /responses, /audit/events, /settings, /organization/current/usage e /organization/current/limits. Todos retornam JSON sem stack trace, respeitam Authorization e usam correlationId em erro.
