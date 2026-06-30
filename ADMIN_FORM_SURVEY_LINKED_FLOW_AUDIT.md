# Auditoria — fluxo administrativo de formulários vinculados a pesquisas

Data: 2026-06-30

## Arquivos auditados

- `app.js`: renderização, botões e jornadas administrativas de formulários/pesquisas.
- `firebase-repository.js`: facade Firebase/Functions, sanitização e CRUD pontual.
- `repository.js`: facade pública `ValoraRepository` que expõe o provider selecionado.
- `functions/index.js`: Cloud Functions administrativas e sanitização backend.
- `firestore.rules`: regras de acesso Firestore; CRUD admin sensível deve preferir Functions.
- `package.json`: scripts de validação e build.
- `scripts/build-production.js`: geração do bundle de produção/hosting.

## Mapeamento de formulários

- `renderForms`/`renderFormList`: a tabela de formulários exibe ações de criar pesquisa, prévia, editar, clonar e excluir.
- `renderFormEditor`/builder: `openFormEditor`, `renderBuilder`, `saveBuilder` controlam edição e gravação.
- `saveForm`/`saveBuilder`: agora consulta `adminGetFormUsage` antes de salvar formulário existente e diferencia alteração textual de alteração estrutural.
- `editForm`: abre o builder; não chama `deleteForm`.
- `previewForm`: só abre a prévia local do formulário.
- `cloneForm`: usa `adminCloneForm`, preserva pesquisas antigas e abre o clone para edição.
- `deleteForm`: antes de excluir chama `adminGetFormUsage`; se houver pesquisas ativas, abre modal com tabela e ações.
- `createSurveyFromForm`/botão “Criar pesquisa e compartilhar”: usa `adminCreateSurvey` e depois `preparePublicSurveyLink` quando necessário.

## Mapeamento de pesquisas

- `renderSurveys` lista pesquisas com editar, compartilhar, destacar e excluir.
- `renderSurveyEditor`/`openSurveyEditor` abre editor direto da pesquisa.
- `saveSurvey` usa Cloud Function de survey (`adminUpdateSurvey`/`adminCreateSurvey`) via repository, sem excluir formulário.
- `editSurvey` abre o editor da pesquisa e não passa por `deleteForm`.
- `deleteSurvey` usa `adminDeleteSurvey`, que faz arquivamento seguro e preserva responses/form.
- `archiveSurvey` e `closeSurvey` foram reforçadas no backend para revogar links, remover destaque e manter responses/form.
- `preparePublicSurveyLink` continua sendo chamado somente para preparar link público/destaque.

## Como o formulário é bloqueado

A exclusão direta de formulário com pesquisa ativa é bloqueada por `adminDeleteForm`, que retorna `failed-precondition` com `details.code = form_linked_to_active_surveys`, a lista `activeSurveys`, contadores e ações sugeridas. O frontend deixou de transformar esse erro em toast genérico e passou a abrir modal resolutivo.

## Quais pesquisas estão vinculadas

A nova Function `adminGetFormUsage` consulta `surveys` por `formId` e retorna pesquisas agrupadas em:

- `activeSurveys`: `active`, `published`, `open` e não deletadas.
- `closedSurveys`: `closed`, `ended`, `inactive`.
- `archivedSurveys`: `archived` ou `deleted === true`.

Cada item retorna título, status, visibilidade, quantidade de respostas, destaque na home e datas.

## Fluxos corrigidos

1. Excluir formulário vinculado abre modal com tabela de pesquisas ativas.
2. Modal oferece editar, encerrar, arquivar e excluir/arquivar pesquisa com segurança.
3. Rodapé oferece clonar formulário, criar nova versão, encerrar/arquivar pesquisas vinculadas e arquivar formulário após encerrar.
4. Edição de formulário vinculado permite mudanças não estruturais.
5. Mudança estrutural com vínculo/respostas oferece criação de nova versão.
6. Excluir pesquisa arquiva com segurança e não toca no formulário nem nas responses.
7. Criar pesquisa a partir de formulário usa Cloud Function pontual e não altera o formulário original.

## Ações que usam Cloud Functions

- `adminGetFormUsage`
- `adminCloseSurveysByForm`
- `adminCreateSurvey`
- `adminUpdateSurvey`
- `adminDeleteSurvey`
- `adminArchiveSurvey`
- `adminCloseSurvey`
- `adminCreateForm`
- `adminUpdateForm`
- `adminDeleteForm`
- `adminCloneForm`
- `adminCreateFormVersion`
- `repairOfficialFormDocument`

## Ações que ainda usavam Firestore direto

Os fallbacks diretos de `deleteForm` e `deleteSurvey` foram removidos no repository para evitar que regras críticas sejam contornadas. Escritas genéricas ainda existem para sincronização geral do estado, mas não são usadas nos CRUDs pontuais corrigidos.

## Como o sistema evita `undefined`

- Frontend: `deepCleanForFirestore` remove `undefined`, funções e chaves privadas em `createDoc`, `updateDoc` e payloads de Functions.
- Backend: `deepCleanForFirestore` sanitiza payloads de create/update e patches transacionais/batch.
- `repairOfficialFormDocument` reforça `isGlobal: true`, `companyId`/`organizationId: valora-oficial`, `status: active` e não grava mais campo `global` indefinido/falso.

## Como o sistema evita loop de `saveChanges`

- `audit` não chama `saveChanges` nem `persist`; usa logger/repository de auditoria em modo não bloqueante.
- `handleError` não chama `audit`; registra erro em logger/server event quando disponível.
- Flags `isHandlingError` e `isSavingAudit` impedem recursão de erro/auditoria.
- CRUD pontual de formulário/pesquisa não usa `saveChanges` geral.
