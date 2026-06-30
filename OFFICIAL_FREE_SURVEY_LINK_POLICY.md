# OFFICIAL_FREE_SURVEY_LINK_POLICY

Sprint 72 define que links demo nunca são processados em produção. O fluxo oficial usa pesquisa gratuita real com token público, submit via Cloud Functions, fallback Firestore e API externa por último. Tokens completos e segredos não devem ser documentados.

Deploy correto:

- `npm run functions:deploy`
- `npm run hosting:deploy`
