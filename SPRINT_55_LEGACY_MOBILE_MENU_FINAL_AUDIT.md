# Sprint 55 — Auditoria final do menu mobile legado

1. O menu administrativo legado é renderizado em `renderPortal(scope, tab)` no `app.js`.
2. A lista administrativa é montada por `getAdminMenuItems(user)`.
3. O filtro por role/permissão é `can(user, item.permission)`.
4. O filtro por módulo marca item como desabilitado via `moduleEnabled(item.module)`; não remove item permitido.
5. O `admin_valora` via mobile via apenas Dashboard porque a arquitetura antiga misturava lista do portal e comportamento mobile, sem uma função explícita compartilhada e auditável para todos os itens administrativos.
6. A correção cobre renderização condicional e CSS: lista única, `height: 100dvh`, `overflow-y:auto`, overlay e z-index.
7. Desktop usa a mesma lista e segue sem overlay.
8. Mobile não usa lista diferente.
9. Mobile reutiliza os itens do desktop em `#adminSidebar`.
10. O botão Menu existe com `data-action="toggleAdminMobileMenu"`.
11. O botão abre a sidebar adicionando `.open`.
12. A sidebar está com `overflow-y:auto!important`.
13. A sidebar usa `height:100dvh` e `max-height:100dvh`.
14. Existe overlay `.admin-mobile-overlay`.
15. Sidebar usa `z-index:1200` e overlay `1190`.
16. Clique em item fecha o menu no mobile.
17. ESC fecha o menu.
18. Resize para desktop fecha o menu.
19. Valora.Web já usa sidebar compartilhada; a paridade é validada por script e teste runtime existente.
20. Gaps documentados: Certificados, Diagnóstico do Ambiente, Diagnósticos gratuitos e Operação aparecem como em implantação no legado quando não há renderizador completo.
