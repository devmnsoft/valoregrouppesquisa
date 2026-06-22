# QA visual automatizado

Esta pasta contém o smoke test visual do Valora Pulse com Playwright.

## Como rodar

```bash
npm install
npx playwright install chromium
npm run test:visual
```

Para depurar com navegador aberto:

```bash
npm run test:visual:headed
```

O servidor local é iniciado pelo `playwright.config.js` com `VALORA_PORT=8095 python server.py`. Para usar um servidor já aberto:

```bash
VISUAL_SKIP_WEBSERVER=1 VISUAL_BASE_URL=http://127.0.0.1:8095 npm run test:visual
```

## Evidências e artefatos

Screenshots semiautomatizados são gravados em `tests/visual/screenshots/` e não devem ser commitados. Falhas, vídeos e traces ficam em `tests/visual/output/`, `test-results/` e `playwright-report/`.

## Cenários cobertos

- Home desktop 1366x768 e mobile 360x800.
- Planos desktop e mobile.
- Pesquisa pública demo por token local.
- Resultado público e certificado em tela.
- Downloads de certificado PDF e PNG.
- ValoraBot sem login, em pesquisa pública e logado como Admin Valora, Empresa Admin e Participante.

## Certificados

O teste abre o resultado demo, clica em `Baixar certificado em PDF` e `Baixar certificado em imagem`, valida extensão, nome seguro, ausência de `undefined` e tamanho maior que zero. A inspeção visual fina do PDF/PNG deve ser feita abrindo os arquivos de download ou screenshots gerados durante uma execução local.

## Limitações

Não há snapshots visuais versionados nesta etapa. O objetivo inicial é smoke visual + evidências, evitando binários no PR. A execução depende dos browsers Playwright instalados no ambiente.
