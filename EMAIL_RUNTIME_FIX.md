# Correção de e-mail e runtime

Causa-raiz: a publicação estática em IIS não possui endpoints `/api/email/*` ou `/api/outbox`. A correção centraliza capacidades em `runtime-capabilities.js`, usa `EMAIL_TRANSPORT` explícito e impede chamadas quando o transporte está desabilitado.

Produção usa `EMAIL_TRANSPORT: 'disabled'`, `LOCAL_API_ENABLED: false` e logs remotos desativados. Local usa `local-outbox` e API local habilitada.

`safeFetchJson` lê texto, valida `content-type` e converte falhas HTML/404 em erro controlado, sem `response.json()` direto nos endpoints locais.
