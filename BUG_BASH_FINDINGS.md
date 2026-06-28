# Bug Bash Findings

## Bugs encontrados nesta preparação
- Gate SaaS E2E podia passar sem API live quando VALORA_E2E_LIVE não estava ativo.
- Release candidate não registrava stdout/stderr resumido nem desligava ambiente local após falha crítica.
- Cutover e rollback ainda tinham forte componente documental.

## Bugs corrigidos
- `prod:saas-e2e` agora exige `--live` e falha sem API_BASE_URL/VALORA_API_BASE_URL.
- `prod:saas-e2e:contract` separa validação estática de contrato.
- Release candidate gera relatório incremental mesmo em falha.

## Bugs restantes
- Homologação live depende de API local com dados fixture ou `VALORA_E2E_SURVEY_ID`.
- Testes Playwright são smoke tests de navegador e devem ser expandidos por seletores dedicados.
