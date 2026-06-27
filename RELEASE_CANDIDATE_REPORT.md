# RELEASE_CANDIDATE_REPORT.md

## Sprint 28 - Homologação funcional SaaS

- Produção permanece com Firebase preservado e `DATA_PROVIDER=firebase`; cutover para API exige aprovação e flag explícita.
- PostgreSQL local usa exclusivamente o schema `valorapesquisa`.
- Gates finais reforçados: SaaS readiness, certificado, e-mail, billing, cobertura frontend, E2E SaaS, cutover dry-run, rollback readiness e release candidate com relatório.
- Limitação conhecida: ambientes sem API/PostgreSQL ativos executam validação estática/contratual; validação viva é feita com `VALORA_E2E_LIVE=1 npm run prod:saas-e2e`.
