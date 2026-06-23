# Ambientes de e-mail

## Local com server.py

Use `LOCAL_API_ENABLED:true` e `EMAIL_TRANSPORT:'local-outbox'`. O frontend pode consultar `/api/email/status`, salvar `/api/email/config`, enviar `/api/email/send` e abrir `/api/outbox`. Respostas JSON passam por `safeFetchJson`.

## Produção IIS estática + Firebase Spark

Use `EMAIL_TRANSPORT:'disabled'`. Templates continuam editáveis, mas SMTP, senha, salvar servidor, teste de e-mail e outbox não ficam disponíveis. O app não finge envio: retornos usam `status:'unavailable'`.

## Firebase Blaze futuro

Use `EMAIL_TRANSPORT:'firebase-functions'` e `ENABLE_CLOUD_FUNCTIONS:true`. Segredos ficam no backend/Secret Manager; o navegador usa callables.

## Backend externo futuro

Use `EMAIL_TRANSPORT:'external-api'` e `EXTERNAL_API_BASE_URL`. A API deve ser autenticada e retornar JSON.
