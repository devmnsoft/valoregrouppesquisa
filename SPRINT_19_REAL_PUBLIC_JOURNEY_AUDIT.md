# Sprint 19 — Auditoria da jornada pública real API/PostgreSQL

## Diagnóstico antes da correção
1. `IsDemo` existia em `PublicSurveysController` como ramificação principal de validate/submit.
2. `demo-public-token` existia no mesmo controller e no validador E2E antigo.
3. `demo-valora-insight` existia no controller e como seed local em `database/postgresql/012_seed_demo_valora_insight.sql`.
4. `pending-backfill` era retornado como `form.id` no validate real.
5. O cálculo de resultado ficava no controller via `new ValoraInsightCalculator()`.
6. `PublicResultsController` apenas exigia string preenchida e não comparava hash real.
7. `survey`, `company` e `certificate` eram objetos vazios/simplificados no resultado público.
8. A submissão legada salvava `response_answers`, mas dentro de `SurveyRepository` e não por serviço público.
9. A submissão legada salvava `result_scores`.
10. A submissão legada salvava `dimension_scores`.
11. A submissão legada criava metadata de certificado simplificada.
12. A submissão legada criava `email_jobs` quando havia e-mail.
13. O controller registrava audit fora da transação.
14. Havia transação no repositório legado, mas a jornada pública não era orquestrada por service.
15. Repositórios principais usavam `valorapesquisa`, com dívida de validação reforçada.
16. Migrations canônicas usam `valorapesquisa`.
17. Scripts de migração mencionam `migration` como pasta/processo, não schema operacional.
18. O frontend já expõe `ValoraRepository` para API/hybrid.
19. `DATA_PROVIDER=api` chama endpoints públicos oficiais via `api-repository.js`.
20. Permanecem termos demo apenas em seed/documentação/testes locais; a jornada real não contém demo hardcoded nos controllers.

## Correção aplicada
Controllers públicos foram afinados, services reais foram adicionados, token de resultado passou a ser gerado e validado por hash, e a submissão pública passou a orquestrar response, answers, scores, dimensions, certificate, email_job, communication e audit_log dentro de transação.
