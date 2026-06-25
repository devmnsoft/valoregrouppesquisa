# SMTP do Communication Gateway

O SMTP fica exclusivamente no backend. O navegador nunca recebe senha, usuário secreto ou chave Firebase Admin.

## Variáveis principais
- `SMTP_ENABLED=true`
- `SMTP_HOST=smtp.seudominio.com.br`
- `SMTP_PORT=587`
- `SMTP_SECURE=false`
- `SMTP_USER=valoragroup@mnsoft.com.br`
- `SMTP_PASS=SENHA_SEGURA_DO_EMAIL`

## Testar
Use `POST /communication/email/test` com `{ "to": "destino@dominio.com" }` ou o script `07-testar-smtp.bat`.
