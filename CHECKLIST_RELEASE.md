# Checklist de release

- [ ] PR aprovada por responsável técnico.
- [ ] Workflow `PR validation` verde.
- [ ] `npm run check` aprovado.
- [ ] `npm run check:no-dist` aprovado.
- [ ] `npm run security:check` aprovado.
- [ ] `npm run build:prod` aprovado.
- [ ] `npm run postbuild:security` aprovado.
- [ ] Nenhum `dist/` commitado.
- [ ] Nenhum source map em `dist/`.
- [ ] Nenhum segredo no código, build ou logs.
- [ ] CSP revisada: sem `script-src *`, `connect-src *` ou `unsafe-eval`.
- [ ] Firebase Rules revisadas.
- [ ] Functions revisadas.
- [ ] App Check revisado.
- [ ] Backup/restauração validados ou plano documentado.
- [ ] Rollback conhecido e aprovado.
- [ ] Deploy de homologação/preview validado.
- [ ] Deploy de produção autorizado via environment `production`.

## Homologação dos pipelines

- [ ] Conferir `HOMOLOGACAO_PIPELINES.md` da release corrente.
- [ ] Confirmar `PR validation` verde em PR real.
- [ ] Confirmar `git ls-files dist` vazio antes do merge.
- [ ] Confirmar artifact `valora-pulse-dist` sem `.map`, `.env`, docs internas ou secrets quando houver run do `Secure production build`.
- [ ] Confirmar preview Firebase somente em PR interno e com secrets de homologação.
- [ ] Confirmar produção somente via `workflow_dispatch` ou tag `v*`, com environment `production` e aprovação manual.
- [ ] Confirmar rollback revisado e responsável definido antes do deploy.
