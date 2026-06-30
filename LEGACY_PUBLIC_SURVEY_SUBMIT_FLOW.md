# Legacy Public Survey Submit Flow

1. `submitSurvey` valida identificação, LGPD e respostas.
2. `submitPublicSurveyResponse` chama `submitPublicSurveyAuto`.
3. Pesquisa gratuita usa `cloud-functions` primeiro.
4. Falha em Functions aciona fallback `external-api`.
5. O erro final só é `provider_unavailable` quando todos os providers falham ou retornam payload inválido.
6. O estado de diagnóstico registra providers, status, código e mensagem sanitizada.
