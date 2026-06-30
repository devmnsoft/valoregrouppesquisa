# Functions Windows Lint Fix

## Causa

O script antigo `node --check index.js && node --check utils/*.js` dependia da expansão de glob pelo shell. No Windows, `utils/*.js` é enviado literalmente ao Node, causando erro de módulo/caminho inexistente.

## Correção

O lint das Functions agora executa `node scripts/check-functions-syntax.js`. O script percorre `index.js` e a pasta `utils` com `fs.readdirSync`, sem glob de shell, e invoca `node --check` com `spawnSync(..., { shell: false })`.

## Comando

```bash
npm run functions:lint
```
