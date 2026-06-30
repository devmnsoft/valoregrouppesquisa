# Sprint 74 — Auditoria cadastro cliente/usuário legado e dist

## Respostas objetivas
1. Formulário de cadastro de cliente: `openCompanyEditor()` em `app.js`, formulário `data-form="company"`.
2. Ao salvar cliente chama `saveCompany(form)`, que agora delega para `handleClientSubmit`/`saveClientAuto` e providers reais.
3. O botão principal é submit do form `company`; o botão de abertura usa `data-action="newCompany"`.
4. Sim: `createActions()` registra `newCompany(){openCompanyEditor();}` e `createFormHandlers()` registra `company:saveCompany`.
5. Sim: chama `repository.createClient`/`updateClient` antes de atualizar estado local.
6. Sim: no provider Firebase chama Cloud Function `createClient` e fallback Firestore `createDoc('organizations')`.
7. Sim: `saveClientViaCloudFunction()` usa callable `createClient`.
8. Não para cadastro real; local state só é atualizado após provider retornar/fallback explícito. O fluxo novo não usa demo como origem real.
9. Formulário de usuário: `openUserEditor()` em `app.js`, formulário `data-form="user"`.
10. Ao salvar usuário chama `saveUser(form)`, que delega para `saveUserAuto`.
11. Sim, preferencialmente via Cloud Function `createUser` com Admin SDK.
12. Sim, `createUser` grava `users/{uid}`; fallback cria `pending_invite` sem senha.
13. Sim, payload valida e preserva `role` com `canCreateUserWithRole`.
14. Sim, `companyId` e `organizationId` são preenchidos com o cliente escolhido ou escopo do admin da empresa.
15. Sim, Functions gera reset link/convite; se falhar mantém `invite_pending` sem senha em claro.
16. Sim, rules permitem admin_valora em `companies`, `organizations` e `users`, e empresa_admin em usuários do próprio cliente.
17. Agora aparece na tela com código sanitizado: `Não foi possível cadastrar... Código: {codigo}`.
18. Sim, `scripts/build-production.js` gera `dist/assets/app.<hash>.js`.
19. Sim, `dist/index.html` referencia `assets/app.<hash>.js` e CSS versionado.
20. O validador antigo não normalizava Windows e não validava referência do HTML de forma robusta; foi trocado por varredura recursiva normalizada.

## Causas raiz
- Cliente: fluxo anterior criava estado local e dependia de sync posterior, sem callable dedicado e com erro real pouco visível.
- Usuário: fluxo anterior podia criar `users` local com senha inicial e sem UID Auth, enquanto o sync Firebase bloqueava novos usuários sem UID.
- Dist: validador antigo não listava arquivos em erro e era frágil para `assets/app.<hash>.js` em paths Windows.

## Correções
- `validate-hosting-dist-build` agora varre `dist` recursivamente, normaliza `\\` para `/`, exige `index.html`, `config.js`, `assets`, JS/CSS versionados e referência no HTML.
- `build-production.js` já gerava `dist/assets/app.<hash>.js`, `style.<hash>.css`, `config.js` e Bootstrap local; mantido.
- Cadastro cliente: adicionadas funções de contrato Sprint 74, validação, provider auto, Cloud Function/Firebase/API fallback e mensagens sanitizadas.
- Cadastro usuário: adicionadas funções de contrato Sprint 74, Auth por Function, fallback `pending_invite`, sem persistir senha em claro.
- Cloud Functions: adicionadas `createClient`, `updateClient`, `createUser`, `updateUserProfile`, `sendUserInvite` com Auth/Admin SDK/auditoria.
- Firestore Rules: mantidas restritivas para admin_valora e empresa_admin do próprio escopo, sem abertura global.

## Deploy recomendado
1. `npm run security:no-secrets`
2. `npm run build:prod`
3. `npm run hosting:dist-build`
4. `npm run functions:deploy`
5. `npm run hosting:deploy`
