# Valora Communication Gateway

Backend Express para e-mail SMTP e WhatsApp Business Cloud API do Valora Pulse™.

## Rodar localmente
```bash
cp .env.example .env
npm install
npm test
npm run start
```

Configure `GATEWAY_API_TOKEN`, SMTP e, opcionalmente, WhatsApp. Logs JSONL são salvos em `logs/communications-YYYY-MM.jsonl` com destinatários mascarados.
