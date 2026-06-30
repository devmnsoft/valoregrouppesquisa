# Sprint 71 — Functions Windows, Node 22, Hosting Dist e Provider Fallback

## Itens auditados

- Lint das Functions sem glob shell (`utils/*.js`).
- Runtime Node.js 22 para Firebase Functions.
- Deploy de Functions com install, readiness e lint antes do Firebase CLI.
- Deploy de Hosting com build de `dist` e validação de assets.
- Hotfix do legado para tentar Cloud Functions, Firestore fallback e API externa antes de `provider_unavailable`.
- Scripts Windows operacionais para deploy assistido.

## SMTP

Nenhuma senha SMTP foi adicionada ao código. A senha deve permanecer em Firebase Secret `SMTP_PASSWORD`.
