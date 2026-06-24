# Deploy Windows do Communication Gateway

1. Copie `communication-gateway/.env.example` para `.env` no servidor.
2. Configure `GATEWAY_API_TOKEN` forte e SMTP (`SMTP_HOST`, `SMTP_PORT`, `SMTP_SECURE`, `SMTP_USER`, `SMTP_PASS`, remetente).
3. Execute `communication-gateway\tools\windows\01-instalar.bat`.
4. Inicie com `communication-gateway\tools\windows\02-iniciar.bat` ou registre como serviço Windows/IIS reverse proxy.
5. Publique com HTTPS em `https://api.valoragroup.mnsoft.com.br`.
6. Valide `GET /health` com `03-testar-health.bat`.
7. Valide envio real com `04-testar-email-resultado.bat` após ajustar token/e-mail.

Se o gateway cair, o frontend registra `failed` ou `pending-provider` e a exibição do resultado continua funcionando.
