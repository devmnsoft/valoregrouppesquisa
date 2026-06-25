# Entrega de e-mails em produção

Configure o frontend com `EMAIL_TRANSPORT: 'external-api'` e `COMMUNICATION_GATEWAY.baseUrl` apontando para `https://api.valoragroup.mnsoft.com.br`.

## SMTP
Copie `communication-gateway/.env.production.example` para `.env` no servidor e preencha `SMTP_HOST`, `SMTP_PORT`, `SMTP_SECURE`, `SMTP_USER`, `SMTP_PASS`, `SMTP_FROM_NAME` e `SMTP_FROM_EMAIL`. Nunca publique o `.env`.

## Teste real
1. Inicie o gateway.
2. Execute `communication-gateway/tools/windows/07-testar-smtp.bat` ou chame `POST /communication/email/test`.
3. Finalize uma pesquisa real com e-mail e confirme `logs/communications-YYYY-MM.jsonl`.

## Logs e falhas
- Fila: `communication-gateway/logs/email-queue.json`.
- Auditoria mensal: `communication-gateway/logs/communications-YYYY-MM.jsonl`.
- E-mails são mascarados nos logs.
