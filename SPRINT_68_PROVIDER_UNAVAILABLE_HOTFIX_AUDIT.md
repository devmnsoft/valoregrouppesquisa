# Sprint 68 — Provider Unavailable Hotfix Audit

## Respostas objetivas
1. `provider_unavailable` era gerado no fluxo público legado quando os providers configurados não retornavam resposta válida e o erro final era normalizado no bloco visual de falha.
2. A função que chama submit da pesquisa pública é `submitSurvey`, que chama `submitPublicSurveyResponse`, que delega para `submitPublicSurveyAuto`.
3. Antes do hotfix, em `auto`, o submit tentava API externa primeiro.
4. Antes do hotfix, a Cloud Function era tentada depois da API externa.
5. O fallback para Cloud Function existia, mas não era priorizado para pesquisa gratuita e dependia de `ValoraFirebaseServices.functions`.
6. `window.firebaseFunctions` não era exposto de forma explícita; agora é preenchido por `firebase-init.js`.
7. `firebase.functions()` está suportado via compat; o helper `getFirebaseFunctionsSafe` centraliza a disponibilidade.
8. `submitSurveyResponse` existe nas Functions.
9. `getPublicResult` existe nas Functions.
10. `sendResultEmail` existe nas Functions.
11. O erro real da API externa era encapsulado em erro genérico; agora fica em `window.ValoraRuntimeDiagnostics.lastPublicSubmit.attempts`.
12. O erro real da Cloud Function era encapsulado; agora fica sanitizado nos diagnostics.
13. A pesquisa gratuita deve usar `publicToken`, `token` ou `accessToken` público real.
14. `tokenHash` não deve aparecer na URL nem ser aceito como token público.
15. A pesquisa gratuita oficial não é bloqueada por `expiresAt` vencido quando não revogada.
16. A correção aplicada foi priorizar `cloud-functions` para pesquisa gratuita, exigir `responseId` e `resultToken` reais, inicializar Functions com segurança, registrar diagnostics sanitizados e manter fallback para API externa.

## Arquivos auditados
- `config.js`: versão 8.7.6, Firebase/Blaze mantido, fallbacks explícitos adicionados.
- `app.js`: submit público auto reordenado para pesquisa gratuita; e-mail, resultado, certificado e erro visual revisados.
- `api-client.js`, `api-repository.js`, `gateway-client.js`: mantidos como providers externos/fallback sem segredo no cliente.
- `firebase-init.js`: inicialização segura de App, Auth, Firestore e Functions.
- `firebase-repository.js`: mantido para repositório Firebase legado.
- `functions/index.js`: callable pública existente e tokenHash não aceito como token público.
- `firebase.json`: Hosting continua apontando para `dist`.
- `package.json`: scripts obrigatórios do hotfix adicionados.
- `scripts/`: validadores estruturais criados/atualizados.
- `tests/e2e/`: spec Playwright do provider hotfix criada.
