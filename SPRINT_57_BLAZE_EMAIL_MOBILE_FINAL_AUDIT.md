# Sprint 57 — Auditoria Blaze, e-mail e menu mobile

1. Sim, `config.js` foi atualizado para `FIREBASE_PLAN='blaze'`.
2. Sim, `ENABLE_CLOUD_FUNCTIONS` está `true`.
3. Sim, `EMAIL_TRANSPORT='auto'` permite API e Cloud Functions.
4. Sim, `PUBLIC_SUBMISSION_PROVIDER='auto'` permite fallback.
5. Sim, `RESULT_PROVIDER='auto'` permite fallback.
6. Sim, `COMMUNICATION_GATEWAY.fallbackToCloudFunctions=true`.
7. Sim, `sendEmail` usa Nodemailer e SMTP.
8. Sim, `SMTP_PASSWORD` está declarado como Firebase Secret.
9. Sim, `EMAIL_BLAZE_PRODUCTION_SETUP.md` documenta PRD.
10. Corrigido: pesquisa gratuita oficial não expira indevidamente por `expiresAt`.
11. Corrigido: `loadValidSurvey` respeita pesquisa grátis válida e mantém bloqueios de status/token/empresa/formulário.
12. Corrigido: fluxo público usa mensagens úteis e registra último erro.
13. Corrigido: e-mail após pesquisa usa `responseId` real retornado pelo provider.
14. Não há `resp_demo` em fluxo produtivo de e-mail; validador criado.
15. Corrigido: menu mobile antigo usa a mesma lista do desktop.
16. Sim, desktop e mobile usam `getAdminMenuItems`.
17. Sim, CSS mobile usa `overflow-y:auto` e `height:100dvh`.
18. Paridade registrada para `Valora.Web` em documento específico.
19. Bloqueios restantes: configurar secrets/variáveis SMTP reais, publicar Functions/Hosting e executar smoke live autenticado.
