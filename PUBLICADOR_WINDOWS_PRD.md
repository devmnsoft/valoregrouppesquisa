# Publicador Windows PRD

## Script Node único
- Simular: `node scripts/publish-production.js --dry-run`
- Publicar app: `node scripts/publish-production.js --apply`
- Publicar app com dados: `node scripts/publish-production.js --apply --with-data`

O publicador valida Spark/Functions, valida Firestore, roda `npm run check`, gera `dist/`, garante `dist/web.config`, copia para `C:\inetpub\wwwroot\valoragroup`, executa health check e grava relatório em `publish/reports`.

## Scripts BAT
Use os arquivos em `tools/windows` para executar cada etapa isoladamente ou o fluxo completo.
