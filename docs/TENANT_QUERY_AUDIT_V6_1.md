# Tenant Query Audit v6.1

| Área | Regra | Status |
|---|---|---|
| User repositories | filtrar por `user_id` e `client_id` | pendente no código HabitFlow ausente |
| Admin repositories | filtrar por `client_id` | pendente no código HabitFlow ausente |
| SuperAdmin repositories | visão global por services próprios | pendente no código HabitFlow ausente |
| Billing tables | possuem `client_id` e índices | garantido na migration 029 |
| Audit/communications | possuem vínculo com cliente/fatura quando aplicável | garantido na migration 029 |

Nenhuma query operacional deve omitir `client_id`, exceto consultas globais isoladas em services SuperAdmin.
