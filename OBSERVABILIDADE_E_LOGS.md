# Observabilidade e Logs — Valora Pulse

A camada central fica em `log-service.js` e expõe `createSystemLog`, `logDebug`, `logInfo`, `logWarn`, `logError`, `logCritical`, `logAudit`, `logSecurity` e `logTest`.

## Níveis
`debug`, `info`, `warn`, `error`, `critical`, `audit`, `security`, `test`.

## Categorias
`auth`, `user`, `company`, `plan`, `module`, `survey`, `form`, `invitation`, `response`, `report`, `action_plan`, `notification`, `billing`, `integration`, `api`, `webhook`, `firebase`, `system`, `security`, `test`.

## Modelo
Cada log registra identificador, nível, categoria, ação, mensagem, empresa, usuário mascarado, entidade, rota, ambiente, versão, metadados sanitizados, erro, stack interna, user agent, status Telegram e data.

## Segurança e LGPD
O serviço mascara e-mails, documentos, senhas, tokens, secrets, cookies, API keys e metadados sensíveis. Telegram recebe payload sanitizado e sem stack completa.

## Destinos
- Local/demo: `state.logs` e console conforme severidade.
- Firebase: painel lê `logs`; Functions podem escrever `logs` e `systemLogs` via Admin SDK.
- Telegram: apenas eventos configurados, principalmente `critical`, `error`, `security` e categorias críticas.

## Painel
Admin Valora acessa **Logs e Monitoramento** com KPIs, filtros, testes manuais, simulações e exportação CSV/JSON.

## Eventos instrumentados nesta evolução
- Login, logout e auditorias existentes passam pelo serviço central.
- Erros tratados e globais do frontend geram `system.frontend_error`.
- Acesso negado gera `security.access_denied`.
- Testes manuais geram logs marcados com `isTest`.
- Exportação de logs registra auditoria.

## Exportação
Exportações CSV/JSON usam dados mascarados e registram auditoria de exportação.
