# Auditoria — correção premium do hero “Diagnóstico gratuito Valora Insight™”

## 1. Onde a seção é renderizada no `app.js`

A seção antiga era renderizada diretamente dentro do template de `renderHome()`, usando `featured-survey-section free-diagnostic-section`, `free-diagnostic-card`, `free-diagnostic-layout`, `free-diagnostic-copy`, `free-diagnostic-preview`, `free-diagnostic-start-card` e `free-diagnostic-preview-card`.

A correção extraiu a seção para `renderFreeDiagnosticHero()`, chamada dentro de `renderHome()` por `${renderFreeDiagnosticHero()}`. A nova seção usa `<section class="free-diagnostic-hero" id="diagnostico-gratuito">`.

## 2. Classes que controlam o lado esquerdo

As classes principais do lado esquerdo agora são:

- `.free-diagnostic-hero__copy`
- `.free-diagnostic-hero__badge`
- `.free-diagnostic-hero__lead`
- `.free-diagnostic-hero__facts`
- `.free-diagnostic-hero__benefits`
- `.free-diagnostic-hero__actions`
- `.free-diagnostic-hero__specialist`

Essas classes substituem a composição antiga baseada em `.free-diagnostic-copy`, `.free-diagnostic-title`, `.free-diagnostic-lead`, `.free-diagnostic-benefits` e `.free-diagnostic-benefit`.

## 3. Classes que controlam o card escuro direito

As classes principais do card direito agora são:

- `.free-diagnostic-hero__preview`
- `.free-diagnostic-preview-card`
- `.free-diagnostic-preview-card__top`
- `.free-diagnostic-preview-card__radar`
- `.free-diagnostic-preview-card__blocker`

O título do card é um texto único: `<h3>Diagnóstico Valora Insight™</h3>`, sem `<br>`, sem letras/palavras em `span` e sem composição vertical.

## 4. Regra CSS que fazia o título quebrar palavra por palavra

A quebra visual vinha da combinação de regras legadas do bloco `.free-diagnostic-start-card, .free-diagnostic-preview-card` e do media query mobile aplicado a `.free-diagnostic-start-card h3, .free-diagnostic-preview-card h3`, principalmente:

- largura estreita no mobile: `width:min(100%,320px); max-width:320px;`
- título muito reduzido no mobile: `font-size:1.1rem;`
- centralização e limites antigos aplicados no mesmo seletor usado pelo card do desktop.

Mesmo sem `word-break: break-all` no bloco encontrado, a largura útil do card/título ficava pequena demais para o texto “Diagnóstico Valora Insight™”, causando quebras artificiais no desktop quando a composição mobile vazava.

## 5. Onde existia `width`/`max-width` estreito demais

O bloco legado no `style.css` usava:

- `.free-diagnostic-start-card, .free-diagnostic-preview-card { width:100%; max-width:460px; ... }`
- no mobile: `.free-diagnostic-start-card, .free-diagnostic-preview-card { width:min(100%,320px); max-width:320px; ... }`
- no mobile: `.free-diagnostic-start-card .btn, .free-diagnostic-preview-card .btn { max-width:240px; ... }`

A nova versão desktop define a coluna direita com `minmax(430px, .92fr)` e o card com `width: min(100%, 470px)`.

## 6. Onde existia `word-break`/`overflow-wrap` inadequado

A auditoria procurou regras agressivas (`word-break: break-all`, `overflow-wrap: anywhere`, `hyphens: auto`) aplicadas aos seletores sensíveis. A correção adicionou regra defensiva explícita para:

- `.free-diagnostic-hero__copy h2`
- `.free-diagnostic-preview-card h3`

com:

```css
word-break: normal;
overflow-wrap: normal;
hyphens: none;
```

## 7. Onde existia regra mobile aplicada no desktop

O problema estava no reaproveitamento das classes antigas `.free-diagnostic-start-card`, `.free-diagnostic-preview-card`, `.free-diagnostic-copy`, `.free-diagnostic-title` e `.free-diagnostic-benefits` em uma seção que deveria ter comportamento desktop-first. Como o mesmo card escuro compartilhava seletores antigos e mobile, a aparência compacta contaminava a percepção desktop.

A nova seção deixa a centralização somente dentro de `@media (max-width: 760px)` para `.free-diagnostic-hero__copy`, `.free-diagnostic-hero__lead`, `.free-diagnostic-hero__actions` e `.free-diagnostic-hero__preview`.

## 8. Correções aplicadas

- Criada `renderFreeDiagnosticHero()` em `app.js` com estrutura premium em duas colunas.
- Substituída a renderização antiga da seção por `${renderFreeDiagnosticHero()}`.
- Adicionados handlers `startFreeDiagnostic` e `scrollHowItWorks`.
- Adicionado CSS desktop-first com grid amplo, coluna direita mínima de `430px` e card escuro de até `470px`.
- Adicionado CSS tablet em `@media (max-width: 1024px)`.
- Adicionado CSS mobile em `@media (max-width: 760px)` com uma coluna, centralização e card compacto.
- Adicionada regra defensiva anti-quebra agressiva para os títulos críticos.
- Criados validadores específicos para estrutura premium, ausência de quebra agressiva, layout desktop e layout mobile.
- Adicionados scripts npm para executar os validadores novos.
