# Changelog


## v6.1-DeepAudit-OperationalCompleteness-ProductionReadiness
- Auditoria profunda do estado real do projeto
- Correção de DI inconsistente documentada como pendente por ausência da árvore HabitFlow
- Sincronização de migrate.sql, script_completo.sql e validate_schema_habitflow.sql
- Inclusion oficial da migration 028 no fluxo de banco
- Migration 029 de fechamento operacional
- Constraint de UserRole ajustada para SuperAdmin
- schema_migrations preparado
- Contratos SuperAdmin para telas operacionais reais
- Contratos para ações SuperAdmin efetivas no banco
- Contratos para Payments, Overdue, Audit e System Health
- BillingCommunicationJob especificado para processamento real de comunicações
- BillingStatusJob especificado para inadimplência real
- EntitlementService e tenant isolation auditados documentalmente
- Scripts de banco e migrations
- Seeds dev/prod revisados
- Script para criar primeiro SuperAdmin
- QA de placeholders, Simple.cshtml, assets e links
- Documentação de produção e operação
