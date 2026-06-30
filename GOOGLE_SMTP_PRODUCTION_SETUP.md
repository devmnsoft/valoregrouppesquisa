# Google SMTP em produção

Remetente: valoragroup@mnsoft.com.br

Variáveis esperadas nas Cloud Functions:

```env
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_SECURITY=starttls
SMTP_USERNAME=valoragroup@mnsoft.com.br
SMTP_SENDER_EMAIL=valoragroup@mnsoft.com.br
SMTP_SENDER_NAME=Valora Group
```

Secret: `SMTP_PASSWORD` configurado via Firebase Secrets.

Comandos:

```bash
firebase functions:secrets:set SMTP_PASSWORD --project gestordepesquisa
firebase deploy --only functions --project gestordepesquisa
firebase deploy --only hosting --project gestordepesquisa
```

Use senha de app do Google/Workspace ou credencial SMTP autorizada. Não use senha normal da conta. Rotacione a senha se ela foi compartilhada em chat, print ou repositório.
