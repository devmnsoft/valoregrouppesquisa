# Sprint 32 — Auditoria Live RC Final

## Respostas objetivas

1. O ambiente live sobe API e PostgreSQL automaticamente? **Sim**, `local:live:up` usa `docker compose up -d --build`.
2. As migrations são aplicadas automaticamente? **Sim**, por `scripts/run-postgres-migrations.js` com `schema_migrations`.
3. Existe seed E2E determinístico? **Sim**, `database/postgresql/099_seed_e2e_live_fixture.sql`.
4. O E2E ainda depende de `VALORA_E2E_SURVEY_ID` manual? **Não** em modo live; usa `/e2e/fixture`.
5. O E2E ainda depende de `VALORA_E2E_PUBLIC_TOKEN` manual? **Não** em modo live; usa `/e2e/fixture`.
6. O E2E trata PDF/PNG corretamente? **Sim**, `requestBinary` valida `application/pdf` e `image/png`.
7. O E2E valida certificate metadata? **Parcial/real**, valida código quando emitido e binários PDF/PNG.
8. O E2E valida email_job? **Sim**, consulta endpoint protegido sem 5xx.
9. O E2E valida audit_log? **Parcial**, coberto por contratos e probes protegidos existentes.
10. O E2E valida usage_monthly? **Sim**, consulta uso da organização autenticada.
11. O E2E valida limite de plano? **Parcial**, contrato exige `PLAN_LIMIT_REACHED`; simulação depende da regra ativa da API.
12. O frontend tem teste real em navegador? **Sim**, Playwright em `tests/e2e`.
13. O release candidate sobe e derruba o ambiente local? **Sim**, `validate-release-candidate` sempre chama `local:live:down` no `finally`.
14. O release candidate gera relatório mesmo em falha? **Sim**, JSON e Markdown são escritos a cada gate.
15. O cutover dry-run executa simulação real local? **Parcial**, valida fluxo e fixture segura; execução com Firebase real exige confirmação explícita.
16. O rollback readiness executa simulação real local? **Parcial**, scripts backup/restore existem; gate valida prontidão documental sem destruir DB automaticamente.
17. O performance baseline roda contra API viva? **Sim**, consulta `/e2e/fixture` e endpoints live.
18. O security-live roda contra API viva? **Sim**, testa auth, payload, CORS e erros.
19. O bug bash tem checklist real e findings? **Sim**, checklist/findings/risk register versionados.
20. O que ainda impede homologação final? Execução completa dos gates em ambiente com Docker/.NET/Playwright disponíveis e evidências manuais de bug bash.

## Termos mapeados

`VALORA_E2E_SURVEY_ID`, `VALORA_E2E_PUBLIC_TOKEN`, `VALORA_E2E_LIVE`, `not set`, `without calling local API`, `--contract`, `--live`, `certificate.pdf`, `certificate.png`, `application/pdf`, `image/png`, `application/json`, `docker compose`, `valora-api`, `valora-postgres`, `seed`, `fixture`, `release-candidate-report`, `SAAS_E2E_REPORT`, `PERFORMANCE_BASELINE`, `CUTOVER_DRY_RUN_REPORT`.
