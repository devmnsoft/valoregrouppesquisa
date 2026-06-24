# Valora Communication Gateway

Gateway Node.js/Express para envio seguro de e-mails transacionais via SMTP, sem expor SMTP no navegador.

## Configuração
```bash
cd communication-gateway
cp .env.example .env
npm install
npm start
```

Configure `.env` com `GATEWAY_API_TOKEN`, `ALLOWED_ORIGINS` e SMTP. Nunca commite `.env`.

## Testes
```bash
npm test
```

## Endpoints
- `GET /health`
- `GET /communication/status`
- `POST /communication/result/send`
- `POST /communication/email/send`

## Segurança
CORS controlado, Helmet, limite JSON, token Bearer, logs JSONL com e-mail mascarado e sem senha/token/HTML completo.
