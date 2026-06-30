# Auditoria CRUD administrativo de pesquisas e formulários

## Causa raiz
O fluxo legado usava `saveChanges` para sincronizar várias coleções após ações pontuais. Assim, editar/excluir uma pesquisa podia tentar salvar todos os formulários, incluindo `forms/form_valora_insight_oficial` com `global: undefined`. O Firestore rejeita `undefined`, abortando a operação real.

## Onde o `undefined` entrava
- Payloads eram propagados sem sanitização profunda em `createDoc`, `updateDoc`, `syncCollectionFromState`, `metadata` e `saveChanges`.
- O formulário oficial não padronizava `isGlobal`, `companyId`, `organizationId` e podia carregar campos legados indefinidos.

## Sincronização excessiva
`saveChanges` sincronizava coleções como `forms`, `surveys`, `companies`, `users`, `settings` e outras em uma única ação. Os fluxos pontuais agora chamam métodos específicos do repositório/Cloud Functions.

## Bloqueio indevido de edição
A mensagem de formulário vinculado pertence somente ao fluxo de exclusão. A edição de formulário vinculado permite alterações não estruturais e bloqueia apenas alterações estruturais, oferecendo versionamento.

## Loop audit/erro
`audit` chamava `save()`, que chamava `saveChanges`. Se `saveChanges` falhasse, o tratamento podia registrar mais auditoria e repetir a falha. `audit` agora é não bloqueante e não sincroniza o estado geral; `save` usa guarda `isHandlingError`.

## Funções alteradas/criadas
- Frontend: `saveBuilder`, `deleteForm`, `saveSurvey`, `deleteSurvey`, `audit`, `save`.
- Repositório Firebase: `deepCleanForFirestore`, `sanitizeStateBeforeSave`, `createDoc`, `updateDoc`, `syncCollectionFromState`, `saveChanges`, métodos admin de survey/form.
- Backend: `adminCreateSurvey`, `adminUpdateSurvey`, `adminDeleteSurvey`, `adminArchiveSurvey`, `adminCloseSurvey`, `adminCreateForm`, `adminUpdateForm`, `adminDeleteForm`, `adminCloneForm`, `adminCreateFormVersion`, `repairOfficialFormDocument`.

## Validadores criados
- `scripts/validate-no-undefined-firestore-writes.js`
- `scripts/validate-admin-survey-crud.js`
- `scripts/validate-admin-form-crud.js`
- `scripts/validate-form-linked-edit-vs-delete.js`
- `scripts/validate-no-savechanges-for-single-crud.js`
- `scripts/validate-audit-no-save-loop.js`

## Reparo operacional
Criado `scripts/repair-official-form-undefined-fields.js` para reparar `forms/form_valora_insight_oficial` via Admin SDK e callable `repairOfficialFormDocument` para reparar formulário, pesquisa, organização e empresa oficiais sem retornar `tokenHash`.
