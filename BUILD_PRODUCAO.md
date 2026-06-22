# Build de produção

A pasta `dist/` é artefato de build e não deve ser commitada no repositório.
Ela é gerada por `npm run build:prod` antes do deploy.

O build de produção:

1. limpa `dist/`;
2. cria `dist/` e `dist/assets/`;
3. gera `dist/index.html`;
4. gera um bundle JavaScript hashado em `dist/assets/app.[hash].js`;
5. gera CSS hashado em `dist/assets/style.[hash].css`;
6. copia os assets originais de `assets/` para `dist/assets/`;
7. executa o `postbuild-security-check`;
8. não gera source maps.

Para gerar localmente:

```bash
npm run build:prod
```

Para deploy no Firebase Hosting:

```bash
npm run build:prod
firebase deploy --only hosting
```

Não commitar a pasta `dist/`.

## Build seguro no CI

O build de produção também é executado nos workflows `.github/workflows/pr-validation.yml` e `.github/workflows/secure-build.yml`.
O pipeline sempre roda `npm run check:no-dist`, `npm run check`, `npm run security:check`, `npm run build:prod`, valida todos os `dist/assets/*.js` com `node --check` e falha se qualquer `*.map` aparecer em `dist/`.

`dist/` continua sendo artefato descartável: pode ser enviado como artifact temporário do GitHub Actions, mas nunca deve ser commitado.
