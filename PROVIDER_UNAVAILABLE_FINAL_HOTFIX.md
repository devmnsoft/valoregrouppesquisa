# PROVIDER_UNAVAILABLE_FINAL_HOTFIX

Documento operacional da Sprint 69.

- Pesquisa gratuita oficial: ordem de providers Cloud Functions -> Firestore emergencial -> API externa.
- provider_unavailable só pode ocorrer após todos os providers falharem ou retornarem resultado inválido.
- Diagnósticos runtime ficam em window.ValoraRuntimeDiagnostics.lastPublicSubmit com mensagens sanitizadas.
- E-mail pós-submit é best-effort e não bloqueia a renderização do resultado/certificado.
- Deploy: npm run functions:deploy e npm run hosting:deploy após os validadores obrigatórios.
