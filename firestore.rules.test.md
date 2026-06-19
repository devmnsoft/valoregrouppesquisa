# Cenários manuais de teste — Firestore Security Rules

Este roteiro valida os pontos sensíveis das regras de segurança para SaaS multiempresa, LGPD e controle de acesso por papéis. Execute preferencialmente no Firebase Emulator Suite com usuários e documentos sem dados reais.

## Massa mínima sugerida

Crie documentos em `users/{uid}` para os perfis abaixo:

- `admin-valora-uid`: `role = admin_valora`, `companyId = valora`.
- `consultor-valora-uid`: `role = consultor_valora`, `companyId = valora`.
- `empresa-a-admin-uid`: `role = empresa_admin`, `companyId = empresa-a`.
- `empresa-a-gestor-uid`: `role = gestor_pesquisa`, `companyId = empresa-a`.
- `empresa-a-participante-uid`: `role = participante`, `companyId = empresa-a`.
- `empresa-b-admin-uid`: `role = empresa_admin`, `companyId = empresa-b`.
- `empresa-b-participante-uid`: `role = participante`, `companyId = empresa-b`.

Crie também:

- `organizations/empresa-a` e `organizations/empresa-b`.
- `forms/form-a` com `companyId = empresa-a`.
- `forms/form-global` com `companyId = global` ou `isGlobal = true`.
- `surveys/survey-a` com `companyId = empresa-a`.
- `responses/response-a` com `companyId = empresa-a` e `participantUid = empresa-a-participante-uid`.
- `invoices/invoice-a` com `companyId = empresa-a`.
- `settings/public` e `settings/private`.

## Users

1. Autenticado como `empresa-a-participante-uid`, tente atualizar `users/empresa-a-participante-uid.role` para `empresa_admin`.
   - Esperado: negado.
2. Autenticado como `empresa-a-participante-uid`, tente atualizar `users/empresa-a-participante-uid.companyId` para `empresa-b`.
   - Esperado: negado.
3. Autenticado como `empresa-a-participante-uid`, atualize somente `name`, `phone`, `preferences` ou `updatedAt` no próprio documento.
   - Esperado: permitido.
4. Autenticado como `empresa-a-admin-uid`, crie usuário em `empresa-a` com `role = participante`.
   - Esperado: permitido.
5. Autenticado como `empresa-a-admin-uid`, crie usuário em `empresa-a` com `role = admin_valora` ou `consultor_valora`.
   - Esperado: negado.
6. Autenticado como `empresa-a-admin-uid`, tente atualizar usuário de `empresa-a` trocando `companyId` para `empresa-b`.
   - Esperado: negado.
7. Autenticado como `empresa-a-admin-uid`, tente atualizar usuário de `empresa-a` trocando `role` para `admin_valora` ou `consultor_valora`.
   - Esperado: negado.
8. Autenticado como `admin-valora-uid`, crie, leia, atualize e exclua usuários.
   - Esperado: permitido.

## Organizations

1. Autenticado como `admin-valora-uid`, crie, leia, atualize e exclua organizações.
   - Esperado: permitido.
2. Autenticado como `consultor-valora-uid`, leia organizações.
   - Esperado: permitido.
3. Autenticado como `empresa-a-admin-uid`, atualize campo operacional de `organizations/empresa-a`, por exemplo `name`.
   - Esperado: permitido.
4. Autenticado como `empresa-a-admin-uid`, tente atualizar campos de cobrança/plano, como `planId`, `billing`, `billingStatus`, `subscriptionId`, `limits` ou `priceId`.
   - Esperado: negado.
5. Autenticado como `empresa-a-admin-uid`, tente ler ou atualizar `organizations/empresa-b`.
   - Esperado: negado.

## Forms

1. Autenticado como `admin-valora-uid`, gerencie qualquer formulário.
   - Esperado: permitido.
2. Autenticado como `consultor-valora-uid`, leia formulários.
   - Esperado: permitido.
3. Autenticado como `empresa-a-admin-uid` ou `empresa-a-gestor-uid`, crie/atualize formulário com `companyId = empresa-a`.
   - Esperado: permitido.
4. Autenticado como `empresa-a-admin-uid` ou `empresa-a-gestor-uid`, tente criar/atualizar formulário com `companyId = empresa-b`.
   - Esperado: negado.
5. Autenticado como `empresa-a-admin-uid` ou `empresa-a-gestor-uid`, leia `forms/form-global`.
   - Esperado: permitido.
6. Autenticado como `empresa-a-admin-uid` ou `empresa-a-gestor-uid`, tente alterar `forms/form-global`.
   - Esperado: negado.
7. Autenticado como participante, tente criar ou editar formulários.
   - Esperado: negado.

## Surveys

1. Sem autenticação, tente ler `surveys/survey-a`.
   - Esperado: negado. Links públicos devem usar Cloud Function.
2. Autenticado como usuário da `empresa-a`, leia `surveys/survey-a`.
   - Esperado: permitido.
3. Autenticado como usuário da `empresa-b`, tente ler `surveys/survey-a`.
   - Esperado: negado.
4. Autenticado como `empresa-a-admin-uid` ou `empresa-a-gestor-uid`, crie/atualize survey com `companyId = empresa-a`.
   - Esperado: permitido.
5. Autenticado como `empresa-a-admin-uid` ou `empresa-a-gestor-uid`, tente alterar `companyId` de survey de `empresa-a` para `empresa-b`.
   - Esperado: negado.

## Responses

1. Sem autenticação, tente criar documento em `responses/{responseId}`.
   - Esperado: negado. Respostas públicas devem ser criadas por Cloud Function.
2. Autenticado como participante da `empresa-a`, tente criar resposta com `companyId = empresa-b`.
   - Esperado: negado.
3. Autenticado como `empresa-a-participante-uid`, leia `responses/response-a` quando `participantUid` for o próprio UID.
   - Esperado: permitido.
4. Autenticado como participante da `empresa-b`, tente ler `responses/response-a`.
   - Esperado: negado.
5. Autenticado como `empresa-a-admin-uid` ou `empresa-a-gestor-uid`, leia respostas de `empresa-a`.
   - Esperado: permitido. A anonimização deve continuar sendo aplicada no frontend/backend.
6. Autenticado como perfil não admin Valora, tente atualizar ou excluir resposta.
   - Esperado: negado.

## Logs

1. Autenticado como `admin-valora-uid`, leia logs.
   - Esperado: permitido.
2. Autenticado como qualquer outro perfil, leia logs.
   - Esperado: negado.
3. Autenticado como qualquer perfil, tente criar, atualizar ou excluir logs pelo frontend/client SDK.
   - Esperado: negado. Logs devem ser gravados por backend/Cloud Function com Admin SDK.

## Invoices

1. Autenticado como `admin-valora-uid`, crie, leia, atualize e exclua faturas.
   - Esperado: permitido.
2. Autenticado como `empresa-a-admin-uid`, leia `invoices/invoice-a`.
   - Esperado: permitido.
3. Autenticado como `empresa-a-admin-uid`, tente alterar `invoices/invoice-a`.
   - Esperado: negado.
4. Autenticado como `empresa-b-admin-uid`, tente ler `invoices/invoice-a`.
   - Esperado: negado.

## Settings

1. Sem autenticação, leia `settings/public`.
   - Esperado: permitido.
2. Sem autenticação ou com perfil que não seja `admin_valora`, leia `settings/private`.
   - Esperado: negado.
3. Autenticado como perfil que não seja `admin_valora`, tente escrever em `settings/public` ou `settings/private`.
   - Esperado: negado.
4. Autenticado como `admin-valora-uid`, leia/escreva em `settings/private`.
   - Esperado: permitido.

## Sugestão de automação com Firebase Emulator

Use `@firebase/rules-unit-testing` para transformar os cenários acima em testes automatizados. Estrutura sugerida:

1. Inicializar o ambiente com `initializeTestEnvironment({ projectId, firestore: { rules } })`.
2. Popular massa inicial com `testEnv.withSecurityRulesDisabled(...)`.
3. Criar contextos autenticados com `testEnv.authenticatedContext(uid)` para cada papel.
4. Validar permissões com `assertSucceeds(...)` e `assertFails(...)`.
5. Executar localmente com `firebase emulators:exec --only firestore "npm test"`.

Priorize asserts para os critérios de aceite: alteração de `role`, troca de `companyId`, criação direta de `responses`, leitura pública de `surveys` e escrita em `logs`.
