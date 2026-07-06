# Paridade visual pública legado → ASP.NET

| Item | Legado | ASP.NET Web | Status |
|---|---|---|---|
| Topbar | `header#topbar` com navegação pública | `_PublicTopbar.cshtml` | migrado |
| Hero | chamada comercial Valora | Home com hero, CTAs e card de jornada | migrado |
| Cores | variáveis de marca no CSS legado | `valora-public.css` com `--brand`, `--accent`, `--surface` | migrado |
| Tipografia | sistema sans e hierarquia comercial | CSS público com títulos fluidos | migrado |
| Botões | CTA diagnóstico/WhatsApp | `.btn-public`, `.btn-whatsapp` | migrado |
| Cards | benefícios, resultado, certificado | `.public-card`, `.result-card`, `.certificate-card` | migrado |
| Espaçamentos | seções amplas mobile-first | `.section`, grids responsivos | migrado |
| Responsividade | mobile-first | menu colapsável, grids 1 coluna, CTA full-width | migrado |
| Footer | footer público completo | `_PublicFooter.cshtml` | migrado |
| Modal | camada modal/confirm | `_PublicModalLayer.cshtml` | migrado |
| Toast | zona viva | `_PublicToastZone.cshtml` + JS | migrado |
| Ações flutuantes | WhatsApp/bot | `_PublicFloatingActions.cshtml` | migrado |
| Chatbot | ValoraBot legado | painel visual sem Firebase | parcial |
| Jornada mobile | sem sidebar | `_PublicLayout` sem sidebar | migrado |
