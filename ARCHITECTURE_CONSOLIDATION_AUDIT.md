# Auditoria de Consolidação da Arquitetura

## Diagnóstico do repositório

A auditoria considerou `package.json`, `config.js`, `runtime-capabilities.js`, `index.html`, `app.js`, `firebase-init.js`, `firebase-repository.js`, `repository.js`, `functions/`, `communication-gateway/`, `scripts/` e `tests/`.

## Respostas obrigatórias

1. Firebase Auth: login, sessão atual, claims e usuários continuam em `firebase-init.js`, `firebase-repository.js` e no fluxo de autenticação do `app.js`.
2. Firestore: organizações, usuários, formulários, pesquisas, respostas, comunicações e configurações continuam carregados pelo repositório Firebase.
3. Cloud Functions: chamadas legadas permanecem encapsuladas por `firebaseCallable`/`callPublicFunction` para `validateSurveyLink`, `submitSurveyResponse`, `sendSurveyInvitations`, `getEmailStatus` e logs.
4. External API/gateway: `EXTERNAL_API_BASE_URL`, `COMMUNICATION_GATEWAY` e providers públicos apontam para o gateway/API quando configurados.
5. Pesquisa pública: validação centralizada em `validatePublicSurveyLink`, escolhendo API, Firebase Functions habilitada ou validação local.
6. Resposta pública: envio centralizado em `submitPublicSurveyResponse`, escolhendo API/gateway, Firebase Functions habilitada ou fallback local.
7. Resultado: cálculo legado no frontend e Cloud Functions; backend adiciona `ValoraInsightCalculator` como fonte final futura.
8. Certificado: legado no frontend; backend adiciona contrato em `CertificateService` e endpoint futuro 501 JSON.
9. E-mail: hoje via gateway externo/Firebase Functions/outbox; backend adiciona jobs e templates sem expor SMTP ao frontend.
10. Migrar primeiro: planos, organizações, usuários, pesquisas públicas, respostas, resultados, comunicações e auditoria.
11. Riscos: dependência de dados Firestore, regras/claims existentes, ausência de PostgreSQL em produção, compatibilidade de links públicos e e-mail transacional.
12. Ordem segura: manter Firebase padrão, espelhar dados no PostgreSQL, validar API em paralelo, usar `hybrid`, depois ativar `api` por módulo.

## Ocorrências mapeadas

Termos auditados: `DATA_PROVIDER`, `API_BASE_URL`, `EMAIL_TRANSPORT`, `EXTERNAL_API_BASE_URL`, `COMMUNICATION_GATEWAY`, `callPublicFunction`, `firebaseCallable`, `submitSurveyResponse`, `validateSurveyLink`, `sendSurveyInvitations`, `getEmailStatus` e `logServerEvent`. As principais ocorrências estão em `config.js`, `config/config.production.js`, `runtime-capabilities.js`, `app.js`, `functions/index.js`, `functions/utils/logging.js`, `log-service.js`, `scripts/` e `tests/`.

## Regras de evolução

- `DATA_PROVIDER=firebase`: preserva produção atual.
- `DATA_PROVIDER=api`: usa ASP.NET Core/PostgreSQL para módulos preparados.
- `DATA_PROVIDER=hybrid`: compara API e Firebase sem corte Big Bang.
