# Auditoria — consistência pesquisa destacada / formulário da home

## Escopo auditado
Arquivos revisados: `functions/index.js`, `app.js`, `firebase-repository.js`, `repository.js`, `local-repository.js`, `data-normalization.js` e `package.json`.

## Backend
- `ensureOfficialFreeSurveyFallback` agora aceita `{ markFeatured = false }`, garante documentos oficiais, mas só marca `official_free_survey` como destaque quando solicitado e quando `hasRealFeaturedSurvey(true)` não encontra pesquisa real válida.
- `hasRealFeaturedSurvey` valida destaque real com status publicável, visibilidade pública, token público, form existente/não arquivado, company ativa/trial e rejeita o fallback oficial.
- `preparePublicSurveyDocument` preserva `before.formId`, carrega formulário por esse id, rejeita `survey_form_mismatch`, retorna `form`, `company` e bloco `consistency`, e ao destacar desmarca todas as outras pesquisas, incluindo `official_free_survey`.
- A survey destacada por clique recebe `featuredAt`, `featuredBy`, `featuredSource: "admin_click"`, `homePriority` e `homePinned`.
- `getFeaturedHomeSurvey` passou a ordenar por grupo explícito: pesquisa real manual, pesquisa real destacada, fallback oficial por último; `updatedAt` não permite mais que o fallback vença uma pesquisa real válida.
- `sanitizeFeaturedPayload` bloqueia retorno sem `survey.id`, sem `survey.formId`, sem `form.id` ou com `form.id !== survey.formId` e acrescenta `consistency` ao payload.
- `debugFeaturedHomeSurvey` e `debugFeaturedHomeSurveyConsistency` expõem candidatos, form vinculado, motivos de rejeição e competição do fallback.

## Front-end
- O botão “Destacar na home” usa o `survey.id` do item clicado, valida existência da survey, valida `survey.formId`, busca o formulário em `state.forms` e mostra confirmação com título/id da pesquisa e título/id do formulário.
- A chamada para `preparePublicSurveyLink` usa `{ surveyId: survey.id, featured: true, free: true }` e não monta o retorno localmente.
- O retorno é aceito apenas se `result.survey.id === survey.id`, `result.survey.formId === survey.formId` e `result.form.id === survey.formId`.
- `clearFeaturedHomeSurveyCache(reason)` limpa `ValoraRuntimeCache`, `ValoraRuntimeDiagnostics.lastFeaturedHomeSurvey`, `lastFeaturedHomeSurveyUrl` e `publicSurveyCache`.
- A invalidação de cache foi ligada a destacar/remover destaque, excluir/arquivar/encerrar pesquisas, salvar pesquisa, salvar/excluir/arquivar formulário vinculado e fechar/arquivar pesquisas vinculadas.
- `resolveFeaturedHomeSurveyPublic` rejeita payload remoto inconsistente e grava no `publicSurveyCache` a tupla `survey/form/company/token/url/org/consistency` da mesma resposta remota.
- `renderHome` mostra diagnóstico somente para admin/consultor: pesquisa da home, formulário, origem e ids.
- Criação de pesquisa por formulário salva `sourceFormId`, `formTitleSnapshot` e `formVersionSnapshot`; preparação pública valida retorno compatível.
- Edição de pesquisa preserva `formId` por padrão, bloqueia troca com respostas e confirma troca explícita com o nome do novo formulário.

## Fluxo de IDs
1. Ao clicar em “Destacar na home”, o front captura exatamente `survey.id` do botão (`data-id`) e localiza a pesquisa em `state.surveys`.
2. A survey salva como destaque é a mesma `survey.id` enviada à callable `preparePublicSurveyLink`.
3. O `formId` usado é `survey.formId` antes da preparação; `preparePublicSurveyDocument` não o substitui.
4. `getFeaturedHomeSurvey` carrega `loadFormForSurvey(survey)`, valida `form.id === survey.formId` e só então retorna o payload.
5. A URL pública usa `survey=<surveyId selecionado>`.
6. O cache público usa a chave `publicSurveyCache.set(remote.survey.id, { survey: remote.survey, form: remote.form, ... })` e valida a compatibilidade antes de gravar.

## Entrada do fallback `official_free_survey`
O fallback entra apenas quando nenhuma pesquisa real destacada válida é encontrada. Quando há destaque real válido, a candidata oficial recebe `official_fallback_deprioritized` e não é selecionada, mesmo que tenha `updatedAt` mais recente.

## Pontos antigos de cache
Os caches com risco eram `publicSurveyCache`, `window.ValoraRuntimeCache.featuredHomeSurveyPromise/result/error`, `window.ValoraRuntimeDiagnostics.lastFeaturedHomeSurvey` e `lastFeaturedHomeSurveyUrl`. Todos são limpos por `clearFeaturedHomeSurveyCache(reason)` nas mutações críticas.
