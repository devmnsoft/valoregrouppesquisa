# Sprint 50 — Auditoria Link/Auth/Menu Mobile

1. A mensagem "Link inválido" é gerada em `validatePublicSurveyLinkLocally`, `renderTakeSurvey` e respostas públicas locais.
2. A validação pública usava `survey.token/publicToken/accessToken` no legado e `tokenHash` na Cloud Function `loadValidSurvey`.
3. Havia risco de depender de `tokenHash` no backend; a correção prioriza `publicToken` real e mantém hash apenas como compatibilidade.
4. A pesquisa gratuita destacada podia ser ignorada sem token público; agora `resolveHomeFeaturedSurvey` chama `ensureSurveyPublicLink` para a pesquisa gratuita ativa.
5. A expiração era respeitada por `surveyIsActive/isBetween`; o script estende para 2036 quando necessário.
6. `repair-featured-home-survey` ajudava, mas não era específico para a pesquisa gratuita; foi criado `repair-free-survey-public-link`.
7. Cadastro de cliente cria `organizations`, normaliza slug/plano/status e registra auditoria no fluxo legado/Firebase.
8. Cadastro de usuário agora deve criar Firebase Auth via Cloud Function `createCompanyUser`.
9. `createCompanyUser` cria `users/{uid}` usando o UID do Auth.
10. Existe Cloud Function/Admin SDK para criar usuários: `createCompanyUser`.
11. `signInWithPassword 400` pode representar credenciais inválidas, usuário ausente, API key/Auth indisponível ou provider desabilitado; o front sanitiza os códigos.
12. O app mostra mensagem amigável para 400 via `mapAuthError`.
13. Perfil Firestore é obrigatório; ausência gera `profile-missing`.
14. Status diferente de active gera `inactive-user`.
15. Role é validado contra `ROLE_DEFINITIONS`.
16. `companyId` é preservado para papéis de empresa.
17. Menu admin mobile agora tem botão `toggleAdminMobileMenu`.
18. Menu admin mobile abre, fecha e navega fechando ao clicar no item.
19. Overlay `.admin-mobile-overlay.active` fecha o menu.
20. Valora.Web recebeu paridade com offcanvas, ESC, clique em item e aria-expanded.

## Mapeamento
- Link inválido/Solicite um novo link: `app.js`, `communication-gateway`, `functions/index.js`.
- resolveHomeFeaturedSurvey/buildHomeFeaturedSurveyUrl/ensureSurveyPublicLink: `app.js` e `scripts/featured-home-survey-core.js`.
- tokenHash/publicToken/accessToken/publicUrl/publicLink: `app.js`, `functions/index.js`.
- signInWithPassword/signInWithEmailAndPassword/createUserWithEmailAndPassword/Firebase Auth/users/{uid}/profile-missing/inactive-user: `firebase-repository.js`, `functions/index.js`.
- admin/dashboard/mobile/sidebar/menu/hamburger/toggle/overlay: `app.js`, `style.css`, `backend/Valora.Web`.
