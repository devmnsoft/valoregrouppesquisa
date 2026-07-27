# Schema SaaS e identidade

`banco_completo.sql` é o bootstrap canônico. `migrations/20260726_002_saas_identity_organizations.sql` atualiza bancos existentes sem exclusão de dados. `NULL` em `plan_limits.limit_value` significa ilimitado. As 25 perguntas quantitativas pontuam de 1 a 5; a questão qualitativa é opcional, limitada a 4000 caracteres e marcada para anonimização. Capabilities ficam materializadas como linhas habilitadas ou bloqueadas para tornar a negativa explícita.
