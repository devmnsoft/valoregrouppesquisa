# Checklist de homologação visual

## Viewports
- 1366x768: validar logo centralizada, hero compacto e card alinhado.
- 1920x1080: validar largura máxima e hierarquia.
- 768px: validar empilhamento sem overflow.
- 390px e 360px: validar botões full width, círculo menor e leitura sem peso vertical.

## ValoraBot
- Abrir o bot na Home.
- Perguntar “quais são os planos?”.
- Perguntar “qual plano é melhor para 500 respostas?”.
- Perguntar “como interpreto meu resultado?”.
- Clicar em “Falar com atendente”.

## Publicação IIS
Use `npm run build:prod`, copie `dist` para o diretório IIS, copie `templates/iis/web.config` e execute `iisreset`. Depois rode `node scripts/healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa`.

## Evidências
Screenshots automatizados devem ser gerados em `test-results/screenshots/` pelos testes Playwright.
