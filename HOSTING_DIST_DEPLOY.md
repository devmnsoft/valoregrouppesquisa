# Hosting Dist Deploy

O Firebase Hosting publica a pasta `dist`. O deploy de Hosting deve sempre executar o build de produção antes de publicar.

```bash
npm run hosting:build
npm run hosting:dist-build
firebase deploy --only hosting --project gestordepesquisa
```

O validador confirma `dist/index.html`, `dist/config.js`, `dist/assets`, referências JS/CSS válidas e presença/referência do bridge mobile legado.
