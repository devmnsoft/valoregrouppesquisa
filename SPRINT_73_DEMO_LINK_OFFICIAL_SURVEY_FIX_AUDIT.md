# Sprint 73 — Auditoria link demo, pesquisa oficial e provider

1. `survey_demo` era aceito pela rota pública ao ler `survey` e `token` antes de qualquer bloqueio definitivo.
2. `empresa-exemplo` era aceito como `org` por estar apenas em query string e não impedir o fluxo.
3. O sistema tentava submeter `survey_demo` porque a rota pública chamava carregamento/envio como pesquisa real quando havia `survey` e `token`.
4. `provider_unavailable` é lançado no fim de `submitPublicSurveyAuto`, após providers sem resultado válido.
5. Agora o link demo é bloqueado antes do submit em produção por `isDemoPublicSurveyLink`, `handleDemoPublicSurveyLink` e `resolveProductionPublicSurveyLink`.
6. A pesquisa oficial real é localizada por `loadOfficialFreeSurvey` usando critérios de gratuita, título, status e visibilidade.
7. A pesquisa oficial deve ter `publicToken`, `token` ou `accessToken`; se faltar, o reparo tenta `repairFreeSurveyPublicLink`.
8. O link oficial usa token público e rejeita `tokenHash`.
9. `tokenHash` não deve aparecer na URL oficial e é recusado no builder/validators.
10. O submit oficial gratuito tenta Cloud Functions primeiro.
11. O submit tenta fallback Firestore em segundo lugar para a pesquisa gratuita oficial.
12. O submit tenta API externa por último.
13. O e-mail é tentado após submit por `sendResultEmailAuto` e não bloqueia resultado.
14. O certificado é renderizado por `certificateHtml` após o resultado.
15. A aba Planos permanece coberta por validador e lista free, essential, professional, corporate e enterprise.
16. O menu mobile permanece coberto por bridge e validador funcional.
17. `build:prod` gera `dist`.
18. `hosting:deploy` publica `dist` após validação.
19. Corrigido: versão 8.8.0, bloqueio demo explícito, fallback público documentado e produção Blaze/auto em `config/config.production.js`.
20. Riscos restantes: depende de dados reais no Firestore para existir pesquisa gratuita oficial com token público; E2E completo requer ambiente Firebase/SMTP acessível.
