# Provider Unavailable Final Fix

A submissão pública gratuita usa a ordem resiliente:

1. Cloud Functions
2. Firestore fallback emergencial
3. API externa

`provider_unavailable` só é lançado depois que todos os providers configurados falham ou retornam resultado inválido. O diagnóstico final fica em `window.ValoraRuntimeDiagnostics.lastPublicSubmit`.

```bash
npm run legacy:provider-final-fix
```
