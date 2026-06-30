# Bridge independente do menu administrativo mobile legado

A bridge `legacy-admin-mobile-menu-bridge.js` garante a abertura da sidebar administrativa em mobile mesmo quando o `app.js` falha parcialmente, quando o botão/sidebar são recriados ou quando handlers antigos deixam de receber o clique.

## Como funciona

- Usa JavaScript puro.
- Não depende de jQuery.
- Não depende de Bootstrap JS.
- Não chama funções internas do `app.js`.
- Registra listener delegado no `document` com `capture=true`.
- Detecta o botão por `[data-action="toggleAdminMobileMenu"]`, `.admin-mobile-toggle` ou `[data-admin-mobile-toggle]`.
- Detecta a sidebar por `#adminSidebar`, `.admin-sidebar` ou `[data-admin-sidebar]`.
- Cria `.admin-mobile-overlay` apenas se ainda não existir.
- Expõe `window.ValoraAdminMobileMenuBridge.debug()` para diagnóstico de produção.

## Debug esperado

Antes do clique, `debug()` deve retornar `bound: true`, `hasButton: true` e `hasSidebar: true`. Depois do clique, deve indicar `sidebarClass` contendo `open`, `bodyClass` contendo `mobile-menu-open` e `buttonExpanded: "true"`.
