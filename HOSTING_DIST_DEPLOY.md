# Hosting Dist Deploy

O deploy de Hosting deve executar `npm run build:prod` antes de `firebase deploy --only hosting --project gestordepesquisa`. O build cria `dist`, copia `config.js`, gera bundle JS/CSS, inclui assets locais e preserva `legacy-admin-mobile-menu-bridge.js` quando referenciado pelo HTML legado.
