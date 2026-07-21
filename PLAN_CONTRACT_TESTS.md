# Contratos de teste dos planos

Os testes e validadores devem verificar o catálogo compartilhado em `shared/plan-catalog.json` e a aplicação no backend.

Cenários obrigatórios:
- plano gratuito aceita exatamente o limite mensal e bloqueia a resposta seguinte;
- submissões concorrentes no último saldo não ultrapassam o limite;
- criação/reativação de pesquisa respeita `limits.activeSurveys`;
- criação de gestores, funcionários, e-mails, unidades e empresas adicionais respeita limites;
- `actionPlan` exige `capabilities.actionPlan` e retorna `plan_capability_required` quando ausente;
- gravação direta em coleções protegidas (`forms`, `surveys`, `responses`, `actionPlans`) é negada pelas regras.
