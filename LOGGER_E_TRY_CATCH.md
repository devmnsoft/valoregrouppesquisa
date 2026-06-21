# Logger e try/catch

`logger-service.js` expõe `window.ValoraLogger` com níveis `debug`, `info`, `warn`, `error`, `critical`, `audit`, `security` e `test`.

Os logs usam payload padronizado com empresa, usuário, rota, ambiente, metadados sanitizados e erro sanitizado. Helpers mascaram e-mail, CPF/CNPJ, tokens e URLs com token.

`app.js` inclui `safeRun` e `safeRunAsync`, captura `error` e `unhandledrejection`, mantém `handleError` com mensagem amigável e registra eventos globais sem expor segredos.
