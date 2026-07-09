# Auditoria final — resultado público, e-mail, WhatsApp, planos e notificações

1. A resposta pública é salva em `functions/index.js`, na Function `submitSurveyResponse`, coleção `responses`.
2. `resultToken` é gerado em `submitSurveyResponse` com `createToken(32)`.
3. `resultTokenHash` é salvo no documento da resposta junto de `resultTokenCreatedAt` e `resultAccessEnabled`.
4. `getPublicResult` é implementado em `functions/index.js` como callable pública por token.
5. O 403 ocorria quando o fluxo público dependia de autenticação ou chamava resultado sem token; agora `getPublicResult` não chama `authedUser` e valida somente hash do token.
6. `loadPublicResult` chama `getPublicResult` em `firebase-repository.js` via `callFunction` sem Firebase Auth.
7. Relatório e certificado usam `loadPublicResultBundleForAction` em `app.js`, com fallback para resposta administrativa quando há sessão privada.
8. Relatório/certificado podem usar cache/sessionStorage pelo cache de resultado público salvo após submit e usado em falhas temporárias de carregamento.
9. O participante não deve usar Firebase Auth; a rota pública `#acessar-resultado` usa senha própria por callable.
10. `accessPassword` é coletado no payload público por `buildPublicSurveySubmitPayload` em `app.js`.
11. `accessPasswordHash` deve ser salvo em `responses/{id}.participantAccess.passwordHash`, nunca em texto puro.
12. WhatsApp é renderizado por `publicWhatsappContactUrl`, `whatsappLink` e `openWhatsapp` em `app.js`.
13. Envio/compartilhamento por WhatsApp de pesquisa e resultado está em `shareSurveyWhatsapp` e `shareResultWhatsapp`.
14. Link de pesquisa é gerado no backend por `preparePublicSurveyLink` / `buildPublicSurveyUrl`.
15. Link expirado no celular vinha de token ausente, hash usado como token ou metadados públicos inconsistentes; `preparePublicSurveyDocument` renova token real e datas.
16. Login privado é acionado por `goLogin` e rota `#login`.
17. O botão Entrar falhava quando query pública antiga continuava ativa; agora `goLogin` limpa query e força `#login`.
18. Notificações de onboarding/white label aparecem em `notification-service.js`.
19. Notificações são geradas por `generateNotifications` com dedupe; em produção o front apenas mescla storage, e dismiss grava `dismissedBy`/`dismissedAt`.
20. Planos são definidos em `officialPlansFallback`/`seedPlans` em `app.js`.
21. Limites por plano são aplicados por `getEffectiveCompanyLimits`, `getLimitStatus` e `enforcePlanLimit`.
22. Usuário grátis pode aderir/solicitar upgrade por `openPlanUpgradeModal`, `showPlanUpgradeCTA` e CTAs do plano.
23. `Valora Pulse™` é mantido para plataforma/admin; tela pública usa `PUBLIC_PRODUCT_NAME = Valora Insight™`.
24. Navegação pública usa `Início`, evitando `HOME` em telas públicas.
25. `Invalid date` é evitado por formatadores seguros (`brDate`, `brDay`, `formatPublicDate`) com fallback.
26. Interrogação no radar PDF vinha de caracteres unicode (`█`, `░`, emoji/setas); PDF agora usa `radarBarPdfSafe` ASCII.
27. Certificado PDF é gerado por `certificatePdf` em `app.js` e `createCertificate` em `pdf.js`.
28. E-mail é enviado por `sendResultEmailInternal` em `functions/index.js`.
29. Provider principal: HTTP API (`EMAIL_PROVIDER=http_api`) com fallback SMTP quando configurado.
30. Correções aplicadas: token público obrigatório, rota pública sem auth, acesso por senha, WhatsApp, HTTP API e-mail, radar PDF seguro, motor Valora Insight™, plano/upgrade/limites, notificações deduplicadas, login estável, ações admin com fallback e validadores finais.
