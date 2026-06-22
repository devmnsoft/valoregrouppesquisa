# UX Home Valora Insight™

## Estrutura do hero
A abertura da Home usa `hero-premium` com três camadas: `hero-brand-card` para logo e assinatura institucional, `hero-copy` para proposta de valor e CTAs, e `hero-diagnostic-card` para o diagnóstico essencial.

## Regras visuais
- Logo limitada a 220px no desktop e 160px no mobile.
- Grid desktop em duas colunas, com card do diagnóstico em duas colunas internas.
- Cores institucionais: azul escuro, ciano de destaque e branco para contraste.
- Botões principais: iniciar diagnóstico, ver planos e falar com especialista.

## Responsividade
- Até 980px o conteúdo empilha em uma coluna.
- Até 760px os botões ocupam largura total, o card fica em uma coluna e o círculo de tempo reduz para 144px.

## Como testar
Execute `node scripts/validate-home-ux.js` e, quando Playwright estiver disponível, `npx playwright test tests/visual-home.spec.js`.
