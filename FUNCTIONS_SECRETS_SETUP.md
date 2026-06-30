# Configuração de secrets das Firebase Functions

## Secrets obrigatórios

### SMTP_PASSWORD

- Necessário para envio de e-mail.
- Configurar com:

```bash
firebase functions:secrets:set SMTP_PASSWORD --project gestordepesquisa
```

## Secrets opcionais

### TELEGRAM_BOT_TOKEN

- Opcional.
- Não deve bloquear deploy.
- Usar apenas se `TELEGRAM_ENABLED=true`.
- Configurar apenas se Telegram for usado:

```bash
firebase functions:secrets:set TELEGRAM_BOT_TOKEN --project gestordepesquisa
```

## Variáveis opcionais

```bash
TELEGRAM_ENABLED=false
TELEGRAM_CHAT_ID=
```

Não commite valores reais de tokens, senhas SMTP, app passwords, service accounts ou chaves privadas.
