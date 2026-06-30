# Correção final do menu mobile legado

`legacy-admin-mobile-menu-bridge.js` é independente, carregado após `app.js`, captura clique em fase capture, abre `#adminSidebar/.admin-sidebar`, ativa overlay e classe `mobile-menu-open`, fecha por ESC, overlay e item de menu e expõe `window.ValoraAdminMobileMenuBridge.debug()`.
