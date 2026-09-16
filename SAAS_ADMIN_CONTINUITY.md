# Continuidade — Administração SaaS

## Mapa desta entrega

| Funcionalidade | Implementação canônica reutilizada | Lacuna corrigida | Verificação |
|---|---|---|---|
| Clientes globais | `saas_customers` + `SaasCustomerService` | listagem sem consulta, filtros ou paginação | `SaasCustomerAdministrationTests` |
| Contratação | `saas_customer_modules` | módulos ativos não apareciam na visão global | agregação somente de `enabled=true` |
| Vínculos | `saas_customer_users` | total ativo não aparecia | contagem por cliente e estado `active` |
| Utilização | `saas_customer_audit_events` | atividade era desconhecida | último evento real; ausência exibida como indisponível |
| Privacidade | documento normalizado existente | documento integral era exibido | projeção mascarada na listagem |

A API e a tela global agora consultam no servidor por nome/documento, situação e módulo, com página limitada, total do conjunto filtrado e ordenação com desempate por identificador. A implementação não cria novos tenants, usuários, assinaturas ou permissões.

## Próxima etapa real

A área global ainda encaminha usuários, cobrança e auditoria para shells informativos; esses fluxos devem ser ligados aos repositórios já existentes antes de serem considerados completos. A política comercial de leitura depois de suspensão também continua sem definição explícita e não foi inventada. Validar a consulta em PostgreSQL e executar a suíte .NET em ambiente com o SDK definido por `backend/global.json`.
