# Cache busting do menu administrativo mobile

A Sprint 62 atualiza os assets críticos para `v=8.7.2` em `index.html`:

- `style.css?v=8.7.2`
- `config.js?v=8.7.2`
- `app.js?v=8.7.2`
- `legacy-admin-mobile-menu-bridge.js?v=8.7.2`

## Firebase Hosting

`firebase.json` mantém `index.html` e `config.js` com `no-store` e adiciona regra dedicada `no-store` para `legacy-admin-mobile-menu-bridge.js`. Isso reduz o risco de produção continuar servindo uma bridge antiga após deploy.

## Publicação

Para publicar, rode as validações locais, gere o build/pacote usado pelo projeto e publique com Firebase Hosting conforme o fluxo operacional existente (`firebase deploy --only hosting` quando aplicável ao ambiente autorizado).
