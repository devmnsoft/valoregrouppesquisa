# GitHub Actions

## PR validation

Arquivo: `.github/workflows/pr-validation.yml`.

Executa em pull requests para `main`:

- `npm install`;
- `npm run check:no-dist`;
- `npm run check`;
- `npm run security:check`;
- `npm run build:prod`;
- `node --check` para todos os bundles `dist/assets/*.js`;
- bloqueio de `*.map` em `dist`;
- `npm run postbuild:security`.

O PR falha se `dist/` for commitado, se houver source map, segredo suspeito, CSP insegura ou JavaScript gerado inválido.

## Secure production build

Arquivo: `.github/workflows/secure-build.yml`.

Executa em push para `main` e manualmente. Gera `dist/` em ambiente limpo, roda os mesmos checks e publica o artefato `valora-pulse-dist` por 7 dias. O artefato é para homologação/deploy; não deve voltar para o Git.

## Firebase Hosting preview

Arquivo: `.github/workflows/firebase-preview.yml`.

Executa em PRs internos, nunca em forks. Gera build seguro e publica canal:

```bash
firebase hosting:channel:deploy pr-<numero-do-pr>
```

Requer secrets de homologação. Se os secrets não existirem, o job falha de forma explícita para evitar preview falso.

## Firebase production deploy

Arquivo: `.github/workflows/firebase-production-deploy.yml`.

Executa somente por `workflow_dispatch` ou tag `v*`. Usa environment `production`, permitindo aprovação manual nas configurações do GitHub. Não existe deploy automático de qualquer branch para produção.

## Homologação dos pipelines

Homologação registrada em `HOMOLOGACAO_PIPELINES.md` em 2026-06-22.

Comandos executados no estado limpo:

- `npm run check` — aprovado.
- `npm run check:no-dist` — aprovado.
- `npm run security:check` — aprovado.
- `npm run build:prod` — aprovado.
- `node --check dist/assets/app.*.js` — aprovado.
- `find dist -name "*.map"` — retorno vazio.
- `git ls-files dist` — retorno vazio.
- `npm run postbuild:security` — aprovado.

Cenários negativos simulados e revertidos antes da entrega:

- `dist/teste.txt` forçado no índice Git: `npm run check:no-dist` falhou com a mensagem esperada de bloqueio de `dist/`.
- `dist/assets/app.fake.js.map`: `npm run postbuild:security` falhou por arquivo proibido no build.
- `TELEGRAM_BOT_TOKEN=fake_test_token_should_be_blocked` em arquivo temporário rastreado: `npm run security:check` falhou por segredo fake.
- CSP temporariamente insegura com `script-src *`, `connect-src *` e `unsafe-eval`: `npm run security:check` falhou pelos três padrões proibidos.

Conclusão: pipelines aprovados com ressalvas de execução externa para artifact, preview e deploy real, que dependem de run no GitHub Actions, secrets de homologação e autorização formal.
