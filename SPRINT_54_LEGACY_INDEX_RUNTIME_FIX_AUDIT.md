# Sprint 54 — Auditoria do index legado

## Diagnóstico objetivo

1. Correções do Valora.Web pendentes no legado: fluxo de menu mobile com overlay runtime, política de expiração gratuita tolerante, erro público sanitizado, contrato de cadastro com organização/empresa/settings, uso obrigatório da Function `createCompanyUser`, painel de diagnóstico Auth e e-mail com `responseId` real. Nesta sprint esses contratos foram conferidos por validadores.
2. Correções do legado pendentes no Valora.Web: paridade de home pública, pesquisa gratuita, link público, certificado, e-mail e menu mobile documentada em `LEGACY_INDEX_TO_ASPNET_WEB_CORRECTION_PARITY.md`.
3. O menu administrativo mobile do legado é renderizado em `renderPortal(scope, tab)`.
4. Os itens são calculados por `menuFor(scope)`, roles, módulos e permissões definidos em `role-definitions.js` e `module-definitions.js`.
5. No mobile aparecia apenas Dashboard por combinação de sidebar fora da viewport, altura/overflow insuficientes e ausência de overlay/estado `open` robusto.
6. O clique no botão Menu chama `toggleAdminMobileMenu` via `data-action`.
7. O botão alterna `aria-expanded` em `openAdminMobileMenu` e `closeAdminMobileMenu`.
8. A sidebar recebe classe `open`.
9. O overlay existe por marcação e por `ensureAdminMobileOverlay`, recebendo `active`.
10. O menu fecha ao clicar em item da sidebar.
11. O menu fecha com ESC.
12. O menu fecha ao redimensionar para desktop.
13. A pesquisa gratuita usa `publicToken`, aceitando `token/accessToken` legado; `tokenHash` não é publicado.
14. Pesquisa gratuita oficial não expira indevidamente por `expiresAt` vencido fora de modo estrito.
15. `loadValidSurvey` não deve bloquear pesquisa gratuita oficial ativa por `isBetween` quando há token público válido.
16. O link público é gerado com `survey`, `token` e `org` quando disponível; `org` não deve ser requisito quando `surveyId + token` são válidos.
17. Cadastro de cliente deve criar organization, company compatível e settings padrão.
18. Cadastro de usuário chama `createCompanyUser` quando não há UID existente.
19. Novo usuário é responsabilidade da Cloud Function: Auth, `users/{uid}`, custom claims e reset link.
20. Resultado da pesquisa gratuita salva resposta real e tenta envio por endpoint `/communications/result/{responseId}/send-email`.
21. Bugs bloqueantes antes de produção: dependência de credenciais reais Firebase/SMTP, homologação manual em dispositivo físico e execução completa dos gates de release no ambiente final.

## Arquivos auditados

`index.html`, `app.js`, `style.css`, `config.js`, `firebase-init.js`, `firebase-repository.js`, `repository.js`, `api-client.js`, `api-repository.js`, `gateway-client.js`, `runtime-capabilities.js`, `role-definitions.js`, `module-definitions.js`, `pdf.js`, `firebase.json`, `firestore.rules`, `functions/index.js`, `functions/package.json`, `scripts/`, `tests/e2e/`, `backend/Valora.Web/`, `backend/Valora.Api/`, `package.json` e `tools/windows/`.
