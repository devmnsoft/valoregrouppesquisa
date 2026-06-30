# Guardrails de Custo Blaze
1. O Blaze está ativo e exige operação assistida.
2. Cloud Functions podem gerar custo por invocação.
3. Firestore pode gerar custo por leitura/gravação.
4. Firebase Hosting pode gerar custo por tráfego.
5. E-mail pode gerar custo no provedor SMTP.
6. Criar orçamento no Google Cloud Billing.
7. Criar alertas de orçamento em 50%, 75%, 90% e 100%.
8. Monitorar número de respostas públicas.
9. Monitorar número de tentativas de e-mail.
10. Monitorar erros 4xx/5xx.
11. Monitorar abuso por IP.
12. Aplicar rate limit em Functions públicas.
13. Aplicar idempotencyKey na submissão pública.
14. Evitar retry infinito usando maxAttempts e dead_letter.
15. Evitar smoke test repetitivo em produção; scripts live devem exigir ALLOW_PRODUCTION_* ou retornar SKIPPED_CONTROLLED.
