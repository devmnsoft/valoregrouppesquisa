# Migração Firebase → PostgreSQL
Mapeamento inicial: `companies/organizations → valora.organizations`, `users → valora.users`, `plans → billing.plans`, `forms → valora.forms/questions/options`, `surveys → valora.surveys`, `responses → valora.responses/response_answers/result_scores`.
Nunca migrar senha em texto puro; usuários devem passar por reset ou hash seguro compatível.
