# Segurança do build de produção

Arquivos binários copiados para `dist/assets/` são gerados no build e não entram no PR.
Os assets originais permanecem em `assets/`.

A pasta `dist/` é ignorada pelo Git porque contém HTML, CSS, JavaScript e imagens gerados para publicação. O Firebase Hosting continua apontando para `dist`, mas o diretório deve ser recriado antes de cada deploy com:

```bash
npm run build:prod
firebase deploy --only hosting
```

O `postbuild-security-check` valida o conteúdo gerado após o build e bloqueia:

- source maps (`*.map`);
- `.env`;
- arquivos `serviceAccount`;
- arquivos ou referências a `secrets`;
- possíveis tokens Telegram;
- referências a senha SMTP;
- arquivos fonte originais não hashados;
- `data/outbox` e configuração local de e-mail.

## Bloqueios automatizados de PR

A validação de PR bloqueia:

- `dist/` commitado;
- source maps em `dist/`;
- tokens Telegram com formato suspeito;
- `TELEGRAM_BOT_TOKEN`, `SMTP_PASSWORD`, `serviceAccount`, `private_key`, `.env` e arquivos sensíveis;
- CSP com `script-src *`, `connect-src *` ou `unsafe-eval`.

`style-src 'unsafe-inline'` permanece permitido temporariamente para CSS inline legado e deve ser tratado como risco controlado até refatoração completa.
