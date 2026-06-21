# Checklist de Regressão Técnica — Valora Pulse

**Versão:** Valora Group™ 8.6.4 RC1
**Data:** 2026-06-21

## Validação JavaScript obrigatória

| Comando | Status | Observações |
|---|---|---|
| `node --check app.js` | Aprovado | Executado em 2026-06-21. |
| `node --check config.js` | Aprovado | Executado em 2026-06-21. |
| `node --check local-repository.js` | Aprovado | Executado em 2026-06-21. |
| `node --check firebase-init.js` | Aprovado | Executado em 2026-06-21. |
| `node --check firebase-repository.js` | Aprovado | Executado em 2026-06-21. |
| `node --check repository.js` | Aprovado | Executado em 2026-06-21. |
| `node --check pdf.js` | Aprovado | Executado em 2026-06-21. |

## Outros JavaScript validados

| Arquivo | Status |
|---|---|
| `analytics-service.js` | Aprovado |
| `functions/index.js` | Aprovado |
| `functions/payments/payment-provider.js` | Aprovado |
| `functions/payments/providers/manual-provider.js` | Aprovado |
| `functions/payments/providers/mercadopago-provider.js` | Aprovado |
| `functions/payments/providers/stripe-provider.js` | Aprovado |
| `functions/utils/audit.js` | Aprovado |
| `functions/utils/hash.js` | Aprovado |
| `functions/utils/logging.js` | Aprovado |
| `functions/utils/telegram.js` | Aprovado |
| `functions/utils/validation.js` | Aprovado |
| `log-service.js` | Aprovado |
| `module-definitions.js` | Aprovado |
| `notification-service.js` | Aprovado |
| `report-service.js` | Aprovado |
| `role-definitions.js` | Aprovado |
| `tests/functions/public-functions.test.js` | Aprovado |
| `tests/rules/firestore.rules.test.js` | Aprovado |

## Checklist técnico complementar

| Item | Status | Evidência/Observação |
|---|---|---|
| CSP | Pendente de validação em navegador/Firebase Hosting | Conferir `firebase.json` e console. |
| Headers | Pendente de validação em Hosting | Conferir resposta HTTP publicada. |
| Scripts duplicados | Pendente de inspeção final | Verificar `index.html`. |
| Constantes duplicadas | Pendente de inspeção final | Verificar `config.js` e serviços. |
| Funções usadas antes de inicializar | Pendente de teste runtime | Conferir console no carregamento. |
| Handlers inline | Pendente de hardening | Mapear antes de remover exceções CSP. |
| `console.error` | Pendente de teste runtime | Homologação deve abrir console e registrar erros críticos. |
| Cache/versionamento | Pendente de validação em browser | Confirmar assets com query string da versão. |
