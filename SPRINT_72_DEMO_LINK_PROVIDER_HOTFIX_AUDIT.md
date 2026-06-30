# Sprint 72 — Auditoria do hotfix demo/provider_unavailable

1. `survey_demo` é tratado em `isDemoPublicSurveyLink` e antes de `renderTakeSurvey` em `routeFromLocation`.
2. `empresa-exemplo` é tratado em `isDemoPublicSurveyLink` e bloqueado no mesmo guard de produção.
3. Sim: `resolveProductionPublicSurveyLink` chama `loadOfficialFreeSurvey` e `ensureOfficialFreeSurveyPublicLink`.
4. `resolveHomeFeaturedSurvey` prioriza destaque ativo e fallback gratuito real.
5. `ensureSurveyPublicLink` prioriza `publicToken`, não usa `tokenHash` como token público e gera token local emergencial apenas quando seguro.
6. `buildHomeFeaturedSurveyUrl` usa `publicToken || token || accessToken`.
7. `tokenHash` não é colocado em URL pública; validadores bloqueiam regressão.
8. O submit tenta Cloud Functions primeiro para pesquisa gratuita oficial.
9. O submit tenta Firestore fallback emergencial em seguida.
10. O submit tenta API externa por último para pesquisa gratuita oficial.
11. `provider_unavailable` aparecia porque o link demo era validado como pesquisa real inexistente/sem provider válido.
12. A substituta é a pesquisa oficial gratuita identificada por `isFree`, `planId=free`, `Valora Insight` ou `Diagnóstico gratuito`.
13. A organização oficial é a empresa vinculada à pesquisa gratuita oficial; `empresa-exemplo` é bloqueada.
14. Pesquisa gratuita oficial ignora expiração vencida quando não revogada.
15. `submitSurveyResponse` retorna `responseId` real.
16. `submitSurveyResponse` gera `resultToken` real e salva `resultTokenHash`.
17. Depois do resultado, `submitSurvey` chama `sendResultEmailAuto`.
18. O resultado renderiza certificado por `renderCertificateSection` sem bloquear conclusão.
19. Regressão é impedida por validadores, E2E e script Windows final.
20. Arquivos corrigidos: `app.js`, `config.js`, `package.json`, validators, E2E, docs e script Windows.
