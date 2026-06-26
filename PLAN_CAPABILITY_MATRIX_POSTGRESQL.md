# Matriz de Capacidades dos Planos — PostgreSQL

A seed `database/postgresql/010_seed_official_plans.sql` mantém os cinco planos oficiais (`free`, `essential`, `professional`, `corporate`, `enterprise`) em `billing.plans`, com limites em `billing.plan_limits` e capacidades em `billing.plan_capabilities`.

| Plano | Limites principais | Capacidades principais |
|---|---|---|
| free | activeSurveys=1, responsesPerMonth=10, managers=1, organizations=1, units=0 | individualResult=full, summaryInsight=full, simpleCertificate=full, resultEmail=limited, resultWhatsApp=manual |
| essential | activeSurveys=3, responsesPerMonth=150, managers=2 | strategicInsight=full, basicReport=full, resultEmail=full, resultWhatsApp=manual |
| professional | activeSurveys=12, responsesPerMonth=1000, managers=8 | executiveReport=full, actionPlans=full, resultEmail=full |
| corporate | activeSurveys=-1, responsesPerMonth=10000, managers=50, units=-1 | multipleUnits=full, consolidatedReports=full, resultEmail=full, resultWhatsApp=full |
| enterprise | activeSurveys=-1, responsesPerMonth=-1, managers=-1, organizations=-1, units=-1 | multipleOrganizations=full, customBranding=full, strategicMeeting=service, consultativeReport=service, executiveFollowUp=service |
