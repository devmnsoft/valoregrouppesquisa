# Provider Unavailable Hotfix

A causa raiz foi a ordem de providers no fluxo legado: a pesquisa gratuita em modo `auto` tentava a API externa antes das Cloud Functions. Quando a API externa estava indisponível e Functions não estavam expostas de forma robusta no front, o usuário recebia `provider_unavailable`.

A solução prioriza `cloud-functions` para pesquisa gratuita, mantém `external-api` como fallback, registra tentativas em `window.ValoraRuntimeDiagnostics.lastPublicSubmit` e só considera sucesso com `responseId` e `resultToken` reais.
