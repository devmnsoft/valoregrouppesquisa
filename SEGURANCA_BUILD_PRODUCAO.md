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

## Erro: `spawnSync git ENOENT`

Esse erro acontece quando o script de segurança tenta executar `git ls-files`, mas o Git não está instalado ou não está no `PATH`. No Windows, teste:

```powershell
git --version
```

Para instalar pelo Windows Package Manager:

```powershell
winget install --id Git.Git -e --source winget
```

Garanta que este caminho esteja configurado no `PATH` do Windows:

```text
C:\Program Files\Git\cmd
```

Sem Git, o `security-check` local avisa sobre a limitação e percorre os arquivos do projeto ignorando diretórios pesados como `node_modules`, `.git`, `dist`, `test-results` e `playwright-report`. Esse modo fallback continua procurando arquivos e conteúdos sensíveis, mas não consegue confirmar se um arquivo está versionado. Em CI (`CI=true` ou `GITHUB_ACTIONS=true`), Git é obrigatório para evitar mascarar riscos de segurança.
