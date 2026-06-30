# Sprint 62 — Auditoria da bridge do menu administrativo mobile legado

## Escopo auditado

Arquivos auditados: `index.html`, `app.js`, `style.css`, `config.js`, `firebase.json`, `package.json`, `tests/e2e/` e `scripts/`.

## Respostas objetivas

1. O botão Menu existe depois da renderização do `app.js`, dentro de `renderPortal`, e pode ser recriado quando o shell administrativo é renderizado novamente.
2. Sim. O botão possui `data-action="toggleAdminMobileMenu"` e também `data-admin-mobile-toggle`.
3. Sim. A sidebar administrativa mantém `id="adminSidebar"`.
4. Sim. A sidebar mantém `class="sidebar admin-sidebar"`.
5. Sim. A bridge adiciona listener delegado em `document`.
6. A causa provável era interceptação/perda de vínculo em runtime dinâmico ou falha antes do bind; a bridge usa capture para receber o clique antes de handlers em bubble.
7. Não foi identificado erro sintático em `app.js` após a correção; a bridge reduz o impacto de falhas parciais futuras.
8. A bridge usa `capture=true`; o handler legado de `app.js` permanece em bubble.
9. Sim, o botão pode ser recriado após renderizações do portal; a bridge usa event delegation e não depende do nó antigo.
10. Sim, a sidebar pode ser recriada após renderizações do portal; a bridge resolve a sidebar no momento do clique.
11. Sim. `.admin-sidebar.open` aplica `transform: translateX(0) !important` em mobile.
12. Não. O overlay inicia sem pointer-events e fica atrás da sidebar (`1390` vs `1400`).
13. Sim. O botão fica em `1405`, a sidebar em `1400` e o overlay em `1390`.
14. Sim. `index.html` carrega `style.css`, `config.js`, `app.js` e a bridge com `v=8.7.2`.
15. O Firebase permitia cache longo para JS/CSS genéricos; agora `config.js`, `index.html` e a bridge têm regra `no-store` e os assets críticos usam query string nova.
16. Sim. `legacy-admin-mobile-menu-bridge.js` é independente de jQuery, Bootstrap JS e funções internas do `app.js`.
17. Sim. O teste local clica no botão real via Playwright.
18. Há base para smoke real por Playwright/Hosting; a sprint adicionou testes locais e validadores de cache para publicação segura.
19. Sim. `window.ValoraAdminMobileMenuBridge.debug()` existe.
20. Causa raiz mais provável: listener legado dependia de renderização/bind em `app.js` e podia ser perdido ou bloqueado em runtime dinâmico/cache antigo; a correção definitiva é delegação em capture + cache busting + bridge independente.

## Evidência técnica

- Bridge carregada depois de `app.js` com versão `8.7.2`.
- `APP_VERSION` atualizado para `8.7.2`.
- CSS mobile sobrescreve conflitos de `display`, `overflow`, `pointer-events`, `z-index` e `transform`.
- Validadores adicionados para estrutura e cache busting.
- Testes Playwright adicionados para runtime com app e standalone sem app.
