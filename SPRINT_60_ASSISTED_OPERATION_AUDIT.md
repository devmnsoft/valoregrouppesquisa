# Sprint 60 — Auditoria de Operação Assistida
Escopo auditado: config.js, index.html, app.js, style.css, firebase-init.js, firebase-repository.js, api-client.js, api-repository.js, gateway-client.js, firebase.json, .firebaserc, firestore.rules, functions/index.js, functions/package.json, scripts/, tests/e2e/, tests/e2e-web/, backend/Valora.Web/, backend/Valora.Api/, package.json e tools/windows/.

1. O projeto já possui monitoramento de produção? Parcial; painel e validadores foram adicionados, observabilidade externa ainda depende do console.
2. O projeto já possui painel operacional? Sim, Admin > Operação Assistida.
3. Existe controle de custo para Blaze? Sim, documentado em BLAZE_COST_GUARDRAILS.md; orçamento real ainda deve ser criado no Billing.
4. Existe rate limit para pesquisa pública? Sim, Functions usam rateLimit em rotas públicas.
5. Existe proteção contra abuso de envio de e-mail? Sim, rateLimit e permissões.
6. Existe fila de e-mail com retry? Sim, emailJobs e pending_retry.
7. Existe dead-letter para e-mails que falham? Sim, dead_letter por maxAttempts.
8. Existe reenvio manual por responseId real? Sim, resendResultEmail exige admin_valora e responseId real.
9. Existe auditoria LGPD para envio/reenvio? Sim, auditLog.
10. Existe backup Firestore documentado? Sim.
11. Existe rollback documentado? Sim.
12. Existe diagnóstico de config.js live? Sim, via painel e scripts existentes.
13. Existe diagnóstico de Cloud Functions? Sim.
14. Existe diagnóstico de SMTP? Sim, getEmailStatus e painel.
15. Existe diagnóstico da pesquisa gratuita? Sim.
16. Existe diagnóstico do menu mobile? Sim.
17. Existe alerta para link gratuito expirando? Parcial; painel alerta e reparo seguro, alerta externo recomendado.
18. Existe alerta para e-mail falhando? Parcial; fila/dead-letter e painel, alerta externo recomendado.
19. Existe alerta para custos ou uso anormal? Documentado; precisa orçamento real no Billing.
20. Quais riscos ainda bloqueiam produção estável? Configuração real de Billing/Secrets/alertas externos e validação smoke assistida em produção.
