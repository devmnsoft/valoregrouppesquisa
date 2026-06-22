# Health Check PRD e Validação Pós-Publicação

O health check PRD confirma se o Valora Pulse publicado no IIS ficou realmente utilizável, sem depender de segredos e sem considerar a publicação OK quando o `index.html` referencia assets ausentes.

## Comandos principais

```bash
node scripts/healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa --check-firebase
node scripts/healthcheck-prd.js --url http://valoragroup.mnsoft.com.br:8089 --project gestordepesquisa --check-firebase
```

Com validações adicionais:

```bash
node scripts/healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa --check-firebase --check-functions --check-firestore --check-public-survey --public-survey-url "https://valoragroup.mnsoft.com.br/index.html?survey=...&token=..."
```

## O que é validado

- IIS/HTML: HTTP 200, `content-type` HTML, presença de `<script>`, `<link>`, `assets/app.*.js` e `assets/style.*.css`.
- Assets: cada JS/CSS referenciado é baixado e deve retornar HTTP 200, MIME correto, tamanho mínimo e conteúdo que não seja HTML.
- Sintomas IIS: 500.19 indica `web.config` inválido; 403.14 indica ausência de `index.html` ou Default Document.
- Firebase frontend: procura `STORAGE_MODE: 'firebase'`, `FIREBASE_ENABLED: true`, `FIREBASE_CONFIG.projectId` e referências ao SDK/serviços.
- Functions: verifica URL esperada de `getEmailStatus` e registra que callable `onCall` não deve ser validada por `fetch` comum.
- Firestore/Auth: com `--check-firestore`, usa Admin SDK/credencial do ambiente para conferir coleções mínimas, `admin_valora` e survey ativa.
- Navegador/Playwright: quando disponível, abre a home nos viewports 1366x768 e 360x800, procura ValoraBot público e falha com erros críticos de console.
- Pesquisa pública: com `--public-survey-url`, valida o link seguro sem tentar descobrir token quando a arquitetura usa hash.

## Relatórios

Os relatórios são gerados em `publish/reports/`:

- `healthcheck-prd-YYYYMMDD-HHMM.md`
- `healthcheck-prd-YYYYMMDD-HHMM.json`

A pasta `publish/` já é ignorada pelo Git, portanto relatórios não devem ser commitados.

## Integração com publicador IIS

```bash
node scripts/publish-iis-prd.js --iis-path C:\inetpub\wwwroot\valoragroup --mode firebase --apply --health-url https://valoragroup.mnsoft.com.br --project gestordepesquisa
```

Se o health check falhar após a cópia, o publicador marca a publicação como falha, mantém o backup disponível e sugere rollback.

## Erros típicos detectados

- `web.config` inválido ou módulo não suportado.
- `index.html` ausente ou Default Document não configurado.
- `index.html` de um build apontando para assets de outro build.
- JS/CSS retornando `text/html` por fallback/erro/cópia incompleta.
- Firebase habilitado sem config ou com `projectId` divergente.
- Firebase SDK/serviços não carregados.
- Erros de CORS, MIME, 404 de asset, `getEmailStatus` e autenticação no console do navegador.
- Firestore sem dados-base mínimos, planos, módulos, organizações, usuários, formulários ou pesquisas.
- ValoraBot indisponível para acesso público.

## Riscos restantes

- Validações Firestore/Auth dependem de credenciais Admin SDK configuradas no ambiente de execução.
- Callable Functions exigem teste via SDK para validação funcional completa; chamada HTTP comum pode gerar falso positivo de CORS.
- Certificados só podem ser validados automaticamente quando houver resposta/link de resultado disponível.
