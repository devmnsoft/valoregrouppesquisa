# Diagnóstico inicial — Sprint Web Legacy Parity

## 1. Estrutura visual do legado
O legado raiz (`index.html`, `style.css`, `app.js` e serviços auxiliares) usa estrutura pública com skip link, `header#topbar`, `main#app`, footer, camadas de modal/confirm/toast, ValoraBot, ações flutuantes, hero comercial, cards e jornada mobile-first.

## 2. Estrutura visual da Web nova
A Web ASP.NET em `backend/Valora.Web` usava `_Layout.cshtml` como casca administrativa com sidebar, topbar interna, cards genéricos e scripts de módulos autenticados.

## 3. Diferenças de layout
A Home nova exibia aparência de painel; o legado prioriza landing pública, CTA comercial, confiança, diagnóstico e WhatsApp.

## 4. Diferenças de jornada
O legado conduz Home → diagnóstico gratuito → pesquisa → resultado → certificado/e-mail/WhatsApp. A Web nova destacava login e planos.

## 5. Diferenças de componentes
Faltavam partials públicas para topbar, footer, modal, toast, floating actions e bot panel.

## 6. Diferenças de rotas
Rotas públicas oficiais solicitadas (`/diagnostico-gratuito`, `/pesquisa/{id}`, `/resultado/{id}`, `/certificado/{id}`, `/lgpd`, `/contato`, `/whatsapp`, `/entrar`) precisavam ficar explícitas e sem sidebar.

## 7. Diferenças de CSS
`app.css` era administrativo. A identidade pública precisava de CSS próprio com variáveis de marca, hero, cards, timeline e responsividade.

## 8. Diferenças de JavaScript
Scripts públicos precisavam operar sem Firebase, consumindo somente API oficial via HTTP e oferecendo loading, toast, modal, validação e CTAs.

## 9. Funcionalidades públicas faltantes
Home comercial completa, diagnóstico destacado, resultado/certificado públicos com CTA, LGPD pública, contato, WhatsApp, bot visual e validadores.

## 10. Funcionalidades administrativas faltantes
Não foi identificada necessidade de ampliar admin nesta sprint; objetivo é separar admin e público.

## 11. O que será migrado
Layout público, identidade visual, Home pública, rotas públicas, partials, JS público, documentos de paridade, validador e E2E público.

## 12. O que será mantido somente como legado
Dependências Firebase e repositórios legados permanecem apenas na raiz histórica, sem uso pela Web oficial.

## 13. Plano objetivo da sprint
Separar `_PublicLayout`/`_AdminLayout`, criar CSS/JS públicos, migrar páginas públicas API-first, documentar paridade, validar ausência de Firebase no `Valora.Web`, rodar gates e registrar auditoria final.
