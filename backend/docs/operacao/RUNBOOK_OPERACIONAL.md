# Runbook operacional
1. **Triagem:** consulte `/SystemHealth`, correlationId e logs sanitizados; classifique healthy/warning/critical.
2. **Banco:** teste `/health/database`, capacidade e conexões; nunca exponha connection string.
3. **Configuração:** corrija bloqueios no provedor de secrets e reinicie de forma controlada.
4. **Manutenção:** defina `App__MaintenanceModeEnabled=true`; platform_admin e leituras continuam disponíveis, escritas recebem 503 amigável. Reative após smoke.
5. **Backup/restore:** siga o runbook específico e registre eventos/governança.
6. **Incidente:** preserve evidências, comunique impacto sem dados pessoais, aplique rollback se os critérios forem atendidos e acompanhe health até estabilizar.
