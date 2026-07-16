# Auditoria — regressão responsiva do Diagnóstico gratuito Valora Insight™

## 1. Onde a seção é renderizada

A seção pública “Diagnóstico gratuito Valora Insight™” é renderizada em `app.js`, dentro do HTML da home, como `featured-survey-section free-diagnostic-section`. A estrutura corrigida separa o wrapper externo, o grid desktop `.free-diagnostic-layout`, o bloco de texto `.free-diagnostic-copy`, a lista `.free-diagnostic-benefits` e o card/preview `.free-diagnostic-start-card.free-diagnostic-preview-card`.

## 2. Classes CSS mobile adicionadas/relacionadas

As classes responsivas do bloco são:

- `.free-diagnostic-section`
- `.free-diagnostic-card`
- `.free-diagnostic-layout`
- `.free-diagnostic-copy`
- `.free-diagnostic-title`
- `.free-diagnostic-lead`
- `.free-diagnostic-benefits`
- `.free-diagnostic-benefit`
- `.free-diagnostic-preview`
- `.free-diagnostic-start-card`
- `.free-diagnostic-preview-card`

A classe antiga `.free-diagnostic-mobile-card` foi removida do HTML para não comunicar nem aplicar comportamento mobile como base de desktop.

## 3. Regras que afetavam desktop indevidamente

Antes da correção, havia um bloco global fora de media query com comportamento mobile:

- `.free-diagnostic-mobile-card { text-align:center; max-width:360px; margin:0 auto; ... }`
- `.free-diagnostic-copy { text-align:center; margin:0 auto 14px; }`
- `.free-diagnostic-copy h2 { font-size:clamp(1.55rem,7vw,2.05rem); text-align:center; }`
- `.free-diagnostic-copy p { max-width:280px; margin:0 auto; text-align:center; }`
- `.free-diagnostic-benefits { display:grid; grid-template-columns:1fr 1fr; ... }`
- `.free-diagnostic-start-card { width:min(100%,300px); margin:16px auto 0; text-align:center; }`

Essas regras transformavam o desktop em um card pequeno centralizado e quebravam o desenho de hero/premium.

## 4. Onde existia `max-width: 300px/360px` fora de media query

O problema estava no bloco global removido de `style.css`:

- `.free-diagnostic-mobile-card` com `max-width:360px`.
- `.free-diagnostic-start-card` com `width:min(100%,300px)`.

Agora, limites de 300/320/360px ficam apenas dentro de `@media (max-width:760px)` ou `@media (max-width:420px)`.

## 5. Onde existia `text-align:center` fora de media query

O problema estava no bloco global removido de `style.css` para:

- `.free-diagnostic-mobile-card`
- `.free-diagnostic-copy`
- `.free-diagnostic-copy h2`
- `.free-diagnostic-copy p`
- `.free-diagnostic-benefits .pill`
- `.free-diagnostic-start-card`
- `.free-diagnostic-start-card h3`
- `.free-diagnostic-start-card p`
- `.free-diagnostic-start-card .btn`

A base desktop agora usa alinhamento à esquerda em `.free-diagnostic-copy`, títulos, lead e card.

## 6. Onde existia `grid-template-columns: 1fr` fora de media query

Não foi mantido nenhum `grid-template-columns:1fr` global para `.free-diagnostic-layout`. A versão desktop define duas colunas com `minmax(0,1.05fr) minmax(360px,.95fr)`.

## 7. Onde existia centralização que deveria ser só mobile

A centralização indevida estava aplicada globalmente no container antigo `.free-diagnostic-mobile-card`, na copy, nos textos, nos benefícios e no card de início. A centralização agora fica exclusivamente dentro de `@media (max-width:760px)`.

## 8. Correções aplicadas

- Reestruturado o HTML em `app.js` para usar `.free-diagnostic-section`, `.free-diagnostic-card`, `.free-diagnostic-layout`, `.free-diagnostic-copy` e `.free-diagnostic-preview`.
- Removido o uso de `.free-diagnostic-mobile-card` como container principal.
- Adicionado CSS desktop-first em `style.css` com grid de duas colunas, largura ampla, texto à esquerda, benefícios em flex-wrap e card premium de até 460px.
- Movidas regras de centralização, fontes reduzidas, grid 1fr e limites estreitos para `@media (max-width:760px)`.
- Adicionado ajuste para `@media (max-width:420px)` com benefícios em uma coluna.
- Criados validadores para impedir regressões de desktop, garantir escopo mobile, exigir grid desktop de duas colunas e validar classes corrigidas no build/fontes.
- Atualizado `package.json` com scripts dedicados de validação.
