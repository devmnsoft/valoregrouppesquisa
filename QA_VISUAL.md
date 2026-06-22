# QA Visual Valora Pulse

## Data
2026-06-22

## Versão
Valora Pulse 8.6.x, executado em ambiente local de desenvolvimento.

## Ambiente
- Servidor local: `python server.py` ou `npm run serve:local`.
- URL padrão: `http://127.0.0.1:8095`.
- Test runner: Playwright.

## Navegadores
- Chromium via Playwright para smoke inicial.
- Firefox/WebKit podem ser adicionados após estabilização dos cenários.

## Viewports
- Desktop: 1366x768.
- Mobile: 360x800.

## Cenários testados
- Home: hero, CTA comercial, remoção de textos técnicos indevidos e ValoraBot público.
- Planos: cards comerciais, CTAs e ValoraBot sem login.
- Pesquisa pública: header público, Home, Ajuda, ValoraBot, WhatsApp quando configurado e ausência de navegação privada.
- Resultado público/certificado: resultado, índice numérico, certificado em tela e botões PDF/PNG.
- Certificado PDF/PNG: evento de download, extensão, nome seguro e arquivo não vazio.
- ValoraBot: sem login, em pesquisa pública e logado em perfis Admin Valora, Empresa Admin e Participante.
- Portais: acesso indireto pelos logins demo usados no teste do ValoraBot logado.

## Evidências
As evidências são geradas em `tests/visual/screenshots/` durante `npm run test:visual`. O workflow manual publica esses arquivos como artifact, sem versioná-los.

## Problemas encontrados
Nenhum problema funcional foi registrado neste documento. A execução real deve atualizar esta seção com prints e achados de homologação.

## Correções necessárias
A definir após homologação com evidências visuais reais.

## Status final
Aprovado com ressalvas: estrutura e automação preparadas; execução depende de instalação dos browsers Playwright no ambiente.
