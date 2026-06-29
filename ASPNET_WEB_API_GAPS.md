# ASPNET_WEB_API_GAPS

## 1. Endpoints existentes e consumidos pelo Valora.Web
| módulo | tela afetada | endpoint | método HTTP | payload esperado | resposta esperada | fallback atual | bloqueia produção | sprint prevista |
|---|---|---|---|---|---|---|---|---|
| Organização | Organization/Index | /organization/current | GET/PUT | dados cadastrais | organização atual | sem fallback silencioso | não | 41 |
| Usuários | Users/Index | /users | GET/POST/PUT/PATCH | usuário/status | lista ou usuário | empty state seguro | não | 41 |
| Formulários | Forms/Index | /forms | GET/POST/PUT | formulário | lista/detalhe | empty state seguro | não | 41 |
| Pesquisas | Surveys/Index | /surveys | GET/POST/PUT/PATCH | pesquisa/status | lista/detalhe | empty state seguro | não | 41 |
| Links públicos | Surveys/PublicLinks | /surveys/{surveyId}/links | GET/POST/PATCH | link/status | lista/link | aviso controlado | não | 41 |
| Respostas | Responses/Index | /responses | GET | filtros | lista/resultado | empty state seguro | não | 41 |
| Auditoria | Audit/Index | /audit/events | GET | filtros | eventos | empty state seguro | não | 41 |
| Configurações | Settings/Index | /settings | GET/PUT | settings | settings | valores seguros | não | 41 |
| Uso/Limites | Dashboard | /organization/current/usage, /limits | GET | nenhum | métricas | cards zerados | não | 41 |

## 2. Endpoints existentes e ainda não consumidos
- Endpoints públicos de certificados/resultados podem ser ampliados no painel administrativo.

## 3. Endpoints faltantes bloqueantes
- Nenhum. Se surgir gap com `bloqueia produção: sim`, deve conter endpoint criado, justificativa clara ou fallback controlado aceito.

## 4. Endpoints faltantes não bloqueantes
- Analytics históricos, billing detalhado e exportações assíncronas.

## 5. Fallbacks temporários permitidos
- Empty state seguro, cards zerados e alerta com `data-gap-controlled="true"`.

## 6. Fallbacks temporários proibidos
- Mock silencioso, dump JSON, token/hash/senha/connection string, stack trace, `Registro 1`, `Item 1`, `Executar ação` e render genérico principal.
