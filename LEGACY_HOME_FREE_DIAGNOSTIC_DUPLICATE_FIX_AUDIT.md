# Auditoria — correção de duplicidade do diagnóstico gratuito na home

## 1. Hero principal “Valora Insight™”

A hero principal definitiva é renderizada por `renderFreeDiagnosticHero()` em `app.js`. Ela contém o badge “Diagnóstico gratuito”, o título “Valora Insight™”, os facts “5 minutos / 25 perguntas / devolutiva estratégica”, os três cards de benefício, os CTAs e o card escuro premium à direita.

Na home, `renderHome()` mantém uma única chamada a `${renderFreeDiagnosticHero()}`.

## 2. Segunda seção repetida

A duplicidade estava em `renderHome()`, logo abaixo da hero principal, por meio de uma seção `free-diagnostic-strip` com novo CTA e copy sobre responder ao diagnóstico gratuito. Além disso, a home também mantinha uma hero anterior (`home-hero-v3`) com conteúdo de diagnóstico, o que reforçava a repetição visual.

## 3. Funções auditadas

- `renderFreeDiagnosticHero`: permanece como fonte única da área de diagnóstico gratuito premium.
- `renderFreeDiagnosticSection`: não é chamada pela home.
- `renderFreeDiagnosticMobileCard`: não é chamada pela home.
- `renderOfficialFreeSurvey`: não é chamada pela home.
- `renderHomeFreeSurvey`: não é chamada pela home.
- `renderFeaturedHomeSurvey`: não é chamada pela home.
- `homeSection`: não é usada para compor a duplicidade corrigida.
- `landingHero`: não é usada para compor a duplicidade corrigida.

## 4. Função que deve permanecer

`renderFreeDiagnosticHero()` deve permanecer como a única renderização do bloco “Valora Insight™” com card escuro “Diagnóstico Valora Insight™”.

## 5. Função removida ou transformada

A seção duplicada `free-diagnostic-strip` foi removida da home. No lugar, foi criada `renderHowItWorksSection()`, com conteúdo próprio de “Como funciona”, quatro passos e sem repetir o título/card/CTA do diagnóstico gratuito.

## 6. Botões

- `Responder diagnóstico gratuito` continua apontando para `startFreeDiagnostic`, que chama `redirectToFeaturedHomeSurvey()`.
- `Ver como funciona` agora usa `data-action="scrollHowItWorks"`.
- `scrollHowItWorks()` rola para `#como-funciona` e exibe toast informativo se a seção não existir.
- `createActions()` mapeia `scrollHowItWorks`.

## 7. Mobile

A hero mantém as classes responsivas existentes (`free-diagnostic-hero`, `free-diagnostic-hero__inner`, `free-diagnostic-hero__copy`, `free-diagnostic-hero__preview`, `free-diagnostic-preview-card`). A nova seção `how-it-works` possui grid 4 colunas no desktop, 2 colunas até 900px e 1 coluna até 560px.

## 8. Correções aplicadas

1. Removida da home a hero anterior repetitiva (`home-hero-v3`).
2. Removida da home a faixa duplicada `free-diagnostic-strip`.
3. Mantida somente `renderFreeDiagnosticHero()` como bloco principal de diagnóstico gratuito.
4. Criada `renderHowItWorksSection()` com conteúdo distinto.
5. Criada `scrollHowItWorks()` apontando para `#como-funciona`.
6. Mapeada a action `scrollHowItWorks` em `createActions()`.
7. Adicionado CSS premium e responsivo para `.how-it-works-section`.
8. Criados validadores específicos para impedir regressão de duplicidade.
9. Adicionados scripts de validação ao `package.json`.
