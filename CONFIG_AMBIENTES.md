# Configuração por ambiente — Valora Pulse™

O frontend é estático no IIS e usa `config.js`. Não há SMTP, senha, token WhatsApp, service account ou token de gateway no navegador.

## Aplicar perfis

```powershell
node scripts\apply-config.js --env production
node scripts\apply-config.js --env local
node scripts\apply-config.js --env local-firebase
```

## Validar

```powershell
node scripts\validate-config-profile.js --env production
node scripts\validate-config-profile.js --env local
node scripts\validate-config-profile.js --env local-firebase
node scripts\validate-config-profile.js --file config.js
```

Produção usa Firebase real e gateway externo. Local usa localStorage/outbox. Local Firebase usa Firebase real com gateway em `http://127.0.0.1:8097`.
