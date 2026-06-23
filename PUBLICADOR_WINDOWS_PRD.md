# Publicador Windows PRD

## Script Node único
- Simular: `node scripts/publish-production.js --dry-run`
- Publicar app: `node scripts/publish-production.js --apply`
- Publicar app com dados: `node scripts/publish-production.js --apply --with-data`

O publicador valida Spark/Functions, valida Firestore, roda `npm run check`, gera `dist/`, garante `dist/web.config`, copia para `C:\inetpub\wwwroot\valoragroup`, executa health check e grava relatório em `publish/reports`.

## Scripts BAT
Use os arquivos em `tools/windows` para executar cada etapa isoladamente ou o fluxo completo.

## Correção runtime capabilities e e-mail por ambiente

- Local: `server.py` fornece API local, outbox e SMTP opcional.
- PRD Spark: IIS estático + Firebase Auth/Firestore, sem API local, sem Cloud Functions, sem envio automático de e-mail.
- PRD Blaze futuro: Cloud Functions com Secret Manager para e-mail seguro e logs remotos.
- Backend externo futuro: API autenticada para transporte externo.
- Validações: `node scripts/validate-runtime-capabilities.js` e `node scripts/validate-email-environment.js` garantem que PRD Spark não chame `/api/email/*`, `/api/outbox`, `getEmailStatus` ou `logServerEvent`.
