# Capacidades de runtime Valora Pulse

A aplicação agora separa capacidade contratual de capacidade real do ambiente. A fonte central é `runtime-capabilities.js`, carregado logo após `config.js`.

## Matriz

| Ambiente | Storage | Firebase | API local | E-mail | Logs remotos |
|---|---|---|---|---|---|
| Local | local | desabilitado | habilitada | `local-outbox` | desabilitados |
| PRD Spark | firebase | Auth/Firestore | desabilitada | `disabled` | desabilitados |
| PRD Blaze futuro | firebase | Auth/Firestore/Functions | desabilitada | `firebase-functions` | habilitados se configurado |
| Backend externo futuro | firebase ou local | opcional | desabilitada | `external-api` | conforme runtime |

Em PRD Spark, endpoints `/api/email/*`, `/api/outbox`, `getEmailStatus` e `logServerEvent` não são chamados.
