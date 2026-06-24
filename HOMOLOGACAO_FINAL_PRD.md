# Homologação final PRD — Valora Pulse™ / Valora Insight™

Relatório operacional do release candidate final. O validador consolidado é `scripts/validate-release-candidate.js` e grava `test-results/release-candidate.json`.

## Escopo validado
- Runtime de produção em Firebase/Spark, sem dependência obrigatória de Cloud Functions.
- Home, FAQ, pesquisa grátis, devolutiva Valora Insight™, certificados PDF/PNG, comunicação, planos, permissões Admin, ValoraBot e health check.
- Certificados gerados em `test-results/certificates/`.

## Critério de aprovação
A PRD só deve ser publicada quando `npm run check`, todos os validadores individuais, `npm run build:prod` e o health check PRD terminarem sem bloqueadores críticos.
