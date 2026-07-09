# Auditoria final — admin responses, resultado público, e-mail e planos

1. A resposta pública é salva em `functions/index.js`, na Function `submitSurveyResponse`, coleção `responses`.
2. `resultToken` é gerado em `submitSurveyResponse` com `createToken(32)`.
3. `resultTokenHash` é salvo no documento `responses/{responseId}` junto com `resultTokenCreatedAt` e `resultAccessEnabled`.
4. `getPublicResult` é implementado em `functions/index.js` como callable pública por token.
5. O 403 ocorria quando a rota/ação tentava ler resultado sem `resultToken` ou por fluxo admin usando API pública; agora token ausente gera `missing_result_token` e token inválido gera `invalid_result_token`, sem `req.auth`.
6. `loadPublicResult` chama `getPublicResult` em `firebase-repository.js` via `callFunction('getPublicResult',{responseId,resultToken})`.
7. Ações admin que antes reaproveitavam fluxo público ficam isoladas em `adminLoadResponseBundle`, `adminViewResponse`, `adminReportResponsePdf` e `adminCertificatePdf` no `app.js`.
8. Relatório e certificado públicos dependem de `resultToken` via `loadPublicResultBundleForAction`; ações admin usam bundle autenticado/local sem exigir token público.
9. Cache/sessionStorage do resultado fica em `readLastPublicResultFromSession`/`valora:lastPublicResult` no `app.js`.
10. `accessPassword` é coletado no payload público por `buildPublicSurveySubmitPayload`.
11. `accessPasswordHash` deve ser salvo apenas como `participantAccess.passwordHash` em `submitSurveyResponse`.
12. Participante não usa Firebase Auth; acesso posterior usa `getParticipantResultsByPassword`.
13. O botão Entrar é renderizado na home/menu público e direciona para `goLogin`/`#login`.
14. O login privado falhava quando query pública permanecia na URL; `goLogin`/`clearPublicRouteParamsBeforePrivateNavigation` limpam a query pública.
15. WhatsApp é renderizado por `publicWhatsappContactUrl`, `whatsappLink` e botões `openWhatsapp` no `app.js`.
16. Enviar pesquisa por WhatsApp foi restaurado em `shareSurveyWhatsapp`, chamando `preparePublicSurveyLink` antes de abrir `wa.me`.
17. Enviar resultado por WhatsApp foi restaurado em `shareResultWhatsapp`, exigindo `responseId` e `resultToken`.
18. Link público da pesquisa é gerado em `buildPublicSurveyUrl` e preparado em `preparePublicSurveyDocument`/`preparePublicSurveyLink`.
19. O link aparecia expirado no celular quando faltava token real, status/visibilidade pública ou validade futura; agora `preparePublicSurveyDocument` repara token, status, visibilidade, validade, repeat e showResult.
20. Notificações indevidas são controladas por deduplicação/dismiss e bloqueios de rota pública/plano grátis no `app.js`.
21. Planos são definidos no seed/estado do `app.js` por `officialPlansFallback` e estrutura de `plans`.
22. Limites por plano são aplicados por `getEffectiveCompanyLimits`, `getLimitStatus`, `limitAvailable` e `enforcePlanLimit`.
23. Upgrade/adesão aparece em `openPlanUpgradeModal`, `showPlanUpgradeCTA` e seção de plano da empresa.
24. O radar gerava interrogação no PDF por caracteres Unicode; `radarBarPdfSafe` usa ASCII `[#####-----]`.
25. Certificado PDF é gerado por `certificatePdf` e `generateValoraInsightCertificatePdf`.
26. Relatório PDF é gerado por `reportResponsePdf` e `generateValoraInsightReportPdf`.
27. E-mail é enviado em `sendResultEmailInternal`, com SMTP/HTTP API, `email_logs` e status honesto.
28. `Valora Pulse™` permanece em documentação/área privada legada; telas públicas usam `Valora Insight™` via `PUBLIC_PRODUCT_NAME` e validadores.
29. `HOME` foi substituído por `Início` nas telas públicas validadas.
30. `Invalid date` é evitado por `formatPublicDate` e usos em tela/PDF/certificado/e-mail.
31. O card duplicado de “Enquadramento geral sem adoçamento” foi removido ao centralizar a renderização em `renderValoraInsightResultPage` com uma única devolutiva.
32. Correções aplicadas: callable pública por token, geração/retorno de `resultToken`, acesso por senha sem Auth, ações admin sem `getPublicResult` sem token, radar PDF ASCII, relatório/certificado premium, WhatsApp `wa.me`, links públicos não expirados, e-mail com status real, login estável, notificações deduplicadas, planos/upgrade e validadores finais.
