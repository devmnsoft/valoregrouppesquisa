# Configuração local

## Local simples

```powershell
node scripts\apply-config.js --env local
python server.py
```

Usa `STORAGE_MODE: 'local'`, `EMAIL_TRANSPORT: 'local-outbox'` e não chama gateway.

## Local com Firebase + gateway

```powershell
node scripts\apply-config.js --env local-firebase
cd communication-gateway
copy .env.local.example .env
node server.js
```

Use este modo para ensaiar produção com Firestore e SMTP no gateway local.
