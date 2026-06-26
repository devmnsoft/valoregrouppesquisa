# Sprint Jornada Principal — Auditoria Técnica

## Pontos localizados
- `routeFromLocation`: `app.js` — resolve query `survey/token`, `result/rt` e hash antes do roteamento.
- `renderHome`, `resolveHomeFeaturedSurvey`, `resolveFeaturedFreeSurvey`, `buildHomeFeaturedSurveyUrl`, `buildSurveyLink`: `app.js` — Home, destaque gratuito e link público.
- `renderTakeSurvey`, `submitSurvey`, `submitPublicSurveyResponse`, `callPublicFunction`, `firebaseCallable`: `app.js` — abertura, validação e envio público.
- `ENABLE_CLOUD_FUNCTIONS`, `EMAIL_TRANSPORT`, `COMMUNICATION_GATEWAY`, `PUBLIC_SUBMISSION_PROVIDER`: `config.js`, `config/config.production.js`, `runtime-capabilities.js`.
- `dispatchPostSurveyCommunication`: `app.js` — envio/registro pós-pesquisa.
- `buildCertificateViewModel`, `exportCertificateImage`, `ValoraPDF.createCertificate`: `app.js`, `pdf.js`.
- `renderSignup`, `registerCompanyAccount`, `loginUser`: `app.js`, `firebase-repository.js`.
- `toggleMenu`, `createActions`, `createFormHandlers`: `app.js`.

## Diagnóstico
- A pesquisa pública é aberta por `routeFromLocation()` quando existem `?survey=` e `token=`, chamando `renderTakeSurvey()`.
- A resposta era enviada em modo Firebase diretamente por `callPublicFunction('submitSurveyResponse')`, incompatível com Spark quando `ENABLE_CLOUD_FUNCTIONS=false`.
- A causa raiz da chamada indevida era ausência de roteador de provider de submissão pública.
- O PDF do certificado é gerado por `ValoraPDF.createCertificate`; o PNG falhava quando dependia de contexto/layout de tela e quando dados inválidos entravam no certificado.
- O e-mail deveria ser disparado por `dispatchPostSurveyCommunication()` via gateway, sem `/api/email/*` no IIS.
- Cadastro/login falhavam para novos usuários Firebase porque o fluxo local salvava `password` no estado e não criava Auth + `organizations` + `users/{uid}`.
- O menu mobile deixava de funcionar por implementação inline em `createActions()` e CSS conflitante; agora há `toggleMenu(force)` e `closeMobileMenu()` oficiais.
